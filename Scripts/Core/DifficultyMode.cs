namespace CatchMeIfYouCan.Scripts.Core
{

    public enum DifficultyMode
    {
        Casual,
        Epidemic,
        Pandemic
    }

    public class DifficultySettings
    {
        public int MinimaxDepth { get; private set; }

        public int BfsSpreadThreshold { get; private set; }

        public int EpPerHealthyOrgan { get; private set; }

        public int MutationIntervalRounds { get; private set; }

        public bool TelegraphMutations { get; private set; }

        public int StartingInfectionZones { get; private set; }


        public float RlLearningRate { get; private set; }
        public float RlDiscountFactor { get; private set; }
        public float RlExplorationRate { get; private set; }


        public string QTableFilePath { get; private set; }

        private DifficultySettings() { }

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