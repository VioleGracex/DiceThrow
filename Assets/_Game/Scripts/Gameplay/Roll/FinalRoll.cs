namespace BG3DiceSystem.Gameplay.Roll
{
    public enum RollMode
    {
        SingleDie,
        AdvantageTwoDice
    }

    public struct FinalRoll
    {
        public string SkillName;
        public RollMode Mode;
        public int DiceValueA;
        public int DiceValueB;
        public int SelectedDiceValue;
        public int Modifier;
        public int Total;
        public int DifficultyClass;
        public bool IsSuccess;
        public bool IsCriticalSuccess;
        public bool IsCriticalFailure;
        public System.Collections.Generic.List<BG3DiceSystem.Gameplay.Skills.ModifierData> AppliedModifiers;
    }
}
