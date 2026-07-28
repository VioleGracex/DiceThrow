using UnityEngine;

namespace BG3DiceSystem.Gameplay.Skills
{
    [CreateAssetMenu(fileName = "SkillCheck_", menuName = "BG3 Dice System/Skill Check SO")]
    public class SkillCheckSO : ScriptableObject
    {
        [Header("Skill Information")]
        public string SkillName = "Persuasion";
        public int DifficultyClass = 15;
        public int DefaultModifier = 4;
        [TextArea(2, 5)]
        public string Description = "Attempt to convince a creature through diplomacy and tact.";
        public Sprite Icon;
    }
}
