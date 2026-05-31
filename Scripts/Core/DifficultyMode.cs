namespace CatchMeIfYouCan.Scripts.Core
{
    /// <summary>
    /// The three difficulty tiers selectable from the main menu.
    /// </summary>
    public enum DifficultyMode
    {
        Casual,
        Epidemic,
        Pandemic
    }


    public class DifficultySettings
    {
        // ------------------------------------------------------------------
        // Minimax (Phase 4)
        // Higher depth = virus looks further ahead = smarter but slower.
        // Proposal specifies: Casual=2, Epidemic=4, Pandemic=6
        // ------------------------------------------------------------------
        public int MinimaxDepth { get; private set; }

        // ------------------------------------------------------------------
        // BFS Spread (Phase 5)
        // The virus only spreads to a zone if its defenses are BELOW this threshold.
        // Casual=2 (needs weak zones), Pandemic=4 (spreads through heavily defended zones)
        // ------------------------------------------------------------------
        public int BfsSpreadThreshold { get; private set; }

        // ------------------------------------------------------------------
        // EP Regeneration (Phase 2)
        // How much EP each healthy organ generates per round.
        // Casual is generous so the player has resources; Pandemic is tight.
        // ------------------------------------------------------------------
        public int EpPerHealthyOrgan { get; private set; }

        // ------------------------------------------------------------------
        // Mutation Rate (Phase 6)
        // How many rounds between forced mutations.
        // Casual=2 (telegraphed, every other round), Epidemic/Pandemic=1 (every round)
        // ------------------------------------------------------------------
        public int MutationIntervalRounds { get; private set; }

        // ------------------------------------------------------------------
        // Mutation Telegraph (Phase 6)
        // In Casual mode the mutation is shown to the player before it happens.
        // ------------------------------------------------------------------
        public bool TelegraphMutations { get; private set; }

        // ------------------------------------------------------------------
        // Pandemic Entry Points
        // In Pandemic, the virus starts in 3 random zones simultaneously.
        // Casual and Epidemic always start in Lungs only.
        // ------------------------------------------------------------------
        public int StartingInfectionZones { get; private set; }

        // ------------------------------------------------------------------
        // RL Hyperparameters (Phase 3 & 11)
        // LearningRate (alpha): how fast the Q-table updates. Higher = learns faster
        //   but can be unstable. Lower = slower but more stable.
        // DiscountFactor (gamma): how much future rewards matter vs immediate ones.
        //   0.9 means the virus cares a lot about long-term damage.
        // ExplorationRate (epsilon): chance the virus picks a RANDOM action instead
        //   of the best known one. High epsilon = more exploration (tries new things).
        //   Low epsilon = more exploitation (does what it already knows works).
        //   Pandemic has LOW epsilon because we want it to exploit known strategies.
        // ------------------------------------------------------------------
        public float RlLearningRate { get; private set; }
        public float RlDiscountFactor { get; private set; }
        public float RlExplorationRate { get; private set; }

        // ------------------------------------------------------------------
        // Q-Table file path (relative to the game's user:// data directory)
        // Each difficulty has its own Q-table so they learn independently.
        // ------------------------------------------------------------------
        public string QTableFilePath { get; private set; }

        // Private constructor — use the factory method below
        private DifficultySettings() { }

        /// <summary>
        /// Factory method. Returns the correct settings object for the given mode.
        /// Usage: var settings = DifficultySettings.For(DifficultyMode.Pandemic);
        /// </summary>
        public static DifficultySettings For(DifficultyMode mode)
        {
            switch (mode)
            {
                case DifficultyMode.Casual:
                    return new DifficultySettings
                    {
                        MinimaxDepth = 2,
                        BfsSpreadThreshold = 2,   // only spreads through zones with < 2 defenses
                        EpPerHealthyOrgan = 15,  // generous EP regen
                        MutationIntervalRounds = 2,   // mutates every 2 rounds
                        TelegraphMutations = true,
                        StartingInfectionZones = 1,   // Lungs only
                        RlLearningRate = 0.3f,
                        RlDiscountFactor = 0.8f,
                        RlExplorationRate = 0.4f, // explores a lot — less predictable but weaker
                        QTableFilePath = "qtable_casual.json"
                    };

                case DifficultyMode.Epidemic:
                    return new DifficultySettings
                    {
                        MinimaxDepth = 4,
                        BfsSpreadThreshold = 3,
                        EpPerHealthyOrgan = 10,  // standard EP regen
                        MutationIntervalRounds = 1,   // mutates every round
                        TelegraphMutations = false,
                        StartingInfectionZones = 1,
                        RlLearningRate = 0.5f,
                        RlDiscountFactor = 0.85f,
                        RlExplorationRate = 0.2f,
                        QTableFilePath = "qtable_epidemic.json"
                    };

                case DifficultyMode.Pandemic:
                default:
                    return new DifficultySettings
                    {
                        MinimaxDepth = 6,
                        BfsSpreadThreshold = 4,   // spreads even through well-defended zones
                        EpPerHealthyOrgan = 6,   // reduced EP regen — player is starved
                        MutationIntervalRounds = 1,
                        TelegraphMutations = false,
                        StartingInfectionZones = 3,   // 3 random zones simultaneously
                        RlLearningRate = 0.6f,
                        RlDiscountFactor = 0.9f,
                        RlExplorationRate = 0.05f, // almost pure exploitation — does what works
                        QTableFilePath = "qtable_pandemic.json"
                    };
            }
        }
    }
}