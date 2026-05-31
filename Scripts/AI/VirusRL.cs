using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using CatchMeIfYouCan.Scripts.Core;
using Godot;

namespace CatchMeIfYouCan.Scripts.AI
{
    public class VirusRL
    {
        // -----------------------------------------------------------------------
        // Q-Table: state key → (action key → Q-value)
        // -----------------------------------------------------------------------
        private Dictionary<string, Dictionary<string, float>> _qTable;

        // -----------------------------------------------------------------------
        // Tracks the last (state, action) pair so Phase 11b can update it
        // -----------------------------------------------------------------------
        private string _lastStateKey;
        private string _lastActionKey;
        private float _severityBeforeAction;

        private readonly DifficultyMode _difficulty;
        private readonly DifficultySettings _settings;
        private readonly Random _rng;

        // All possible RL actions = the 6 organ zone names (spread targets)
        private static readonly string[] AllActions =
        {
            "Lungs", "Bloodstream", "LymphNodes", "Gut", "Heart", "Brain"
        };

        // -----------------------------------------------------------------------
        // Constructor — loads Q-table from disk (or seeds it if first run)
        // -----------------------------------------------------------------------
        public VirusRL(DifficultyMode difficulty)
        {
            _difficulty = difficulty;
            _settings = DifficultySettings.For(difficulty);
            _rng = new Random();
            _qTable = LoadOrSeedQTable();

            GD.Print($"[RL] Initialized for {difficulty} | " +
                     $"α={_settings.RlLearningRate} γ={_settings.RlDiscountFactor} " +
                     $"ε={_settings.RlExplorationRate}");
        }

        // ===================================================================
        // PHASE 3 — Select Action (Epsilon-Greedy)
        // ===================================================================

        /// <summary>
        /// Selects a spread target using epsilon-greedy policy.
        ///   ε chance  → random action (exploration: try something new)
        ///   1-ε chance → best known action (exploitation: do what works)
        ///
        /// Only considers zones that are actually reachable (adjacent to an
        /// infected zone and not yet infected). Falls back to Minimax target
        /// if RL has no reachable actions.
        ///
        /// Returns the chosen target zone key, or null if no moves available.
        /// </summary>
        public string SelectAction(GameState state, string minimaxSuggestion)
        {
            // Save severity snapshot for the Bellman update in Phase 11b
            _severityBeforeAction = state.SeverityIndex;

            // Build the list of actually reachable uninfected zones
            var reachable = GetReachableTargets(state);

            if (reachable.Count == 0)
            {
                GD.Print("[RL] No reachable targets — deferring to Minimax.");
                _lastStateKey = null;
                _lastActionKey = minimaxSuggestion;
                return minimaxSuggestion;
            }

            string stateKey = EncodeState(state);
            _lastStateKey = stateKey;

            string chosenAction;

            // Epsilon-greedy: explore or exploit?
            if (_rng.NextDouble() < _settings.RlExplorationRate)
            {
                // EXPLORATION: pick a random reachable zone
                chosenAction = reachable[_rng.Next(reachable.Count)];
                GD.Print($"[RL] Exploring → {chosenAction}");
            }
            else
            {
                // EXPLOITATION: pick the reachable zone with the highest Q-value
                EnsureStateExists(stateKey);
                chosenAction = reachable
                    .OrderByDescending(a => GetQValue(stateKey, a))
                    .First();
                GD.Print($"[RL] Exploiting → {chosenAction} " +
                         $"(Q={GetQValue(stateKey, chosenAction):F2})");
            }

            _lastActionKey = chosenAction;

            // If RL and Minimax agree, log it — it means both systems converge
            if (chosenAction == minimaxSuggestion)
                GD.Print("[RL+MINIMAX] Both systems agree on target: " + chosenAction);
            else
                GD.Print($"[RL] Overriding Minimax ({minimaxSuggestion}) → {chosenAction}");

            return chosenAction;
        }

        // ===================================================================
        // PHASE 11b — Update Q-Table (Bellman Equation)
        // ===================================================================

        /// <summary>
        /// Called after the full round resolves. Updates the Q-table entry
        /// for the (state, action) pair chosen in Phase 3 using the Bellman equation:
        ///
        ///   Q(s,a) ← Q(s,a) + α × [R + γ × max Q(s',a') - Q(s,a)]
        ///
        /// Where:
        ///   R        = reward = net change in SeverityIndex this round
        ///   s'       = new state after the round
        ///   max Q(s')= best known Q-value from the new state
        ///   α        = learning rate
        ///   γ        = discount factor (how much future rewards matter)
        /// </summary>
        public void UpdateQTable(GameState newState)
        {
            if (_lastStateKey == null || _lastActionKey == null)
                return;

            // Reward = net severity change this round
            // Positive = severity went up = good for virus
            // Negative = player pushed severity down = bad for virus
            float reward = newState.SeverityIndex - _severityBeforeAction;

            // Get the new state key and the best Q-value from it
            string newStateKey = EncodeState(newState);
            float bestFutureQ = GetBestQValue(newStateKey);

            // Bellman update
            float oldQ = GetQValue(_lastStateKey, _lastActionKey);
            float newQ = oldQ + _settings.RlLearningRate *
                            (reward + _settings.RlDiscountFactor * bestFutureQ - oldQ);

            SetQValue(_lastStateKey, _lastActionKey, newQ);

            GD.Print($"[RL] Q-table update: ({_lastStateKey}, {_lastActionKey}) " +
                     $"{oldQ:F2} → {newQ:F2} | reward={reward:+0;-0} " +
                     $"bestFuture={bestFutureQ:F2}");

            // Persist to disk after every update
            SaveQTable();

            // Reset tracking
            _lastStateKey = null;
            _lastActionKey = null;
        }

        // ===================================================================
        // State Encoding
        // ===================================================================

        /// <summary>
        /// Encodes the current game state into a compact string key.
        /// Format: "I{infectedCount}_S{severityBracket}_E{epBracket}_M{mutationCount}"
        ///
        /// Example: "I2_S1_E2_M1" = 2 infected zones, 25-50% severity,
        ///           40-60 EP, 1 active mutation
        /// </summary>
        private string EncodeState(GameState state)
        {
            int infectedCount = state.OrganGraph.Zones.Values.Count(z => z.IsInfected);
            int severityBracket = state.SeverityIndex < 25 ? 0
                                : state.SeverityIndex < 50 ? 1
                                : state.SeverityIndex < 75 ? 2 : 3;
            int epBracket = state.PlayerEp < 20 ? 0
                                : state.PlayerEp < 40 ? 1
                                : state.PlayerEp < 60 ? 2 : 3;
            int mutationCount = Math.Min(state.ActiveMutations.Count, 3); // cap at 3

            return $"I{infectedCount}_S{severityBracket}_E{epBracket}_M{mutationCount}";
        }

        // ===================================================================
        // Q-Table helpers
        // ===================================================================

        private void EnsureStateExists(string stateKey)
        {
            if (!_qTable.ContainsKey(stateKey))
                _qTable[stateKey] = new Dictionary<string, float>();

            foreach (string action in AllActions)
            {
                if (!_qTable[stateKey].ContainsKey(action))
                    _qTable[stateKey][action] = 0f;
            }
        }

        private float GetQValue(string stateKey, string actionKey)
        {
            if (_qTable.ContainsKey(stateKey) &&
                _qTable[stateKey].ContainsKey(actionKey))
                return _qTable[stateKey][actionKey];
            return 0f;
        }

        private void SetQValue(string stateKey, string actionKey, float value)
        {
            EnsureStateExists(stateKey);
            _qTable[stateKey][actionKey] = value;
        }

        private float GetBestQValue(string stateKey)
        {
            if (!_qTable.ContainsKey(stateKey)) return 0f;
            if (_qTable[stateKey].Count == 0) return 0f;
            return _qTable[stateKey].Values.Max();
        }

        private List<string> GetReachableTargets(GameState state)
        {
            var reachable = new HashSet<string>();
            foreach (var kvp in state.OrganGraph.Zones.Where(z => z.Value.IsInfected))
            {
                if (!state.OrganGraph.AdjacencyList.ContainsKey(kvp.Key)) continue;
                foreach (string n in state.OrganGraph.AdjacencyList[kvp.Key])
                    if (!state.OrganGraph.Zones[n].IsInfected)
                        reachable.Add(n);
            }
            return reachable.ToList();
        }

        // ===================================================================
        // Q-Table Persistence — JSON save/load
        // ===================================================================

        /// <summary>
        /// Resolves the full filesystem path for the Q-table file.
        /// Uses Godot user:// when inside the engine, falls back to working
        /// directory when called from unit tests (no scene tree available).
        /// </summary>
        private string ResolveFullPath()
        {
            try
            {
                // Throws if Godot engine singleton is not initialised (e.g. unit tests)
                return ProjectSettings.GlobalizePath("user://" + _settings.QTableFilePath);
            }
            catch
            {
                return Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    _settings.QTableFilePath
                );
            }
        }

        /// <summary>
        /// Saves the Q-table to disk as JSON. Silent on failure.
        /// </summary>
        private void SaveQTable()
        {
            try
            {
                string json = JsonSerializer.Serialize(_qTable, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(ResolveFullPath(), json);
            }
            catch (Exception e)
            {
                GD.PrintErr($"[RL] Failed to save Q-table: {e.Message}");
            }
        }

        /// <summary>
        /// Loads the Q-table from disk. Falls back to pre-seeded values
        /// if the file is missing or corrupt.
        /// </summary>
        private Dictionary<string, Dictionary<string, float>> LoadOrSeedQTable()
        {
            try
            {
                string fullPath = ResolveFullPath();
                if (File.Exists(fullPath))
                {
                    string json = File.ReadAllText(fullPath);
                    var loaded = JsonSerializer.Deserialize<
                        Dictionary<string, Dictionary<string, float>>>(json);

                    if (loaded != null && loaded.Count > 0)
                    {
                        GD.Print($"[RL] Loaded Q-table ({loaded.Count} states) from {fullPath}");
                        return loaded;
                    }
                }
            }
            catch (Exception e)
            {
                GD.PrintErr($"[RL] Could not load Q-table: {e.Message} — using seeds.");
            }

            GD.Print($"[RL] Seeding new Q-table for {_difficulty}");
            return BuildSeededQTable();
        }

        // ===================================================================
        // Pre-seeded Q-Table Values
        // ===================================================================

        /// <summary>
        /// Builds a Q-table pre-seeded with reasonable starting values.
        ///
        /// The seeds encode domain knowledge about which zones are strategically
        /// valuable to infect:
        ///   Brain     → highest value (proposal: +25% severity on infection)
        ///   Heart     → second (critical zone, leads to Brain)
        ///   LymphNodes → high (cuts off player's main EP source)
        ///   Bloodstream → medium-high (the virus highway — enables everything)
        ///   Gut       → medium
        ///   Lungs     → lowest (already infected at start in most modes)
        ///
        /// Pandemic seeds are 3× more aggressive than Casual seeds.
        /// </summary>
        private Dictionary<string, Dictionary<string, float>> BuildSeededQTable()
        {
            // Seed multipliers per difficulty
            float mult = _difficulty switch
            {
                DifficultyMode.Casual => 0.3f,
                DifficultyMode.Epidemic => 0.6f,
                DifficultyMode.Pandemic => 1.0f,
                _ => 0.6f
            };

            // Base strategic values for each target zone
            var baseValues = new Dictionary<string, float>
            {
                { "Brain",       10.0f },
                { "Heart",        8.0f },
                { "LymphNodes",   7.0f },
                { "Bloodstream",  6.0f },
                { "Gut",          4.0f },
                { "Lungs",        2.0f }  // usually already infected
            };

            // Generate seeds for the most common starting states
            // We seed the early-game states most heavily since those matter most
            var seeded = new Dictionary<string, Dictionary<string, float>>();

            // States: I1 (1 infected zone) with various severity/EP combos
            var earlyStates = new[] { "I1_S0_E3_M0", "I1_S0_E2_M0", "I1_S0_E3_M1" };
            var midStates = new[] { "I2_S1_E2_M1", "I2_S1_E1_M1", "I3_S1_E2_M2" };
            var lateStates = new[] { "I3_S2_E1_M2", "I4_S2_E1_M2", "I4_S3_E0_M3" };

            foreach (string state in earlyStates.Concat(midStates).Concat(lateStates))
            {
                seeded[state] = new Dictionary<string, float>();
                foreach (var kv in baseValues)
                    seeded[state][kv.Key] = kv.Value * mult;
            }

            return seeded;
        }
    }
}