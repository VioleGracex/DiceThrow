using System;

namespace BG3DiceSystem.Core.Interfaces
{
    public enum Language
    {
        EN,
        RU
    }

    public interface ILocalizationService
    {
        Language CurrentLanguage { get; }
        event Action OnLanguageChanged;

        void SetLanguage(Language language);
        string GetText(string key);
        string GetText(string key, params object[] args);
        string GetSkillName(string englishSkillName);
        string GetSkillDescription(string englishSkillName);
        string GetModifierName(string englishModifierName);
    }
}
