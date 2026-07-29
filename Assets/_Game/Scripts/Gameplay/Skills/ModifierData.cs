using System;

namespace BG3DiceSystem.Gameplay.Skills
{
    [Serializable]
    public class ModifierData
    {
        public string Id;
        public string Name;
        public int Value;
        public bool IsRemovable;

        public ModifierData(string name, int value, bool isRemovable = true)
        {
            Id = Guid.NewGuid().ToString("N");
            Name = name;
            Value = value;
            IsRemovable = isRemovable;
        }

        public ModifierData(string id, string name, int value, bool isRemovable = true)
        {
            Id = id;
            Name = name;
            Value = value;
            IsRemovable = isRemovable;
        }
    }
}
