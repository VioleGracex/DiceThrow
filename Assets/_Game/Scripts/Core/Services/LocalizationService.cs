using System;
using System.Collections.Generic;
using UnityEngine;
using BG3DiceSystem.Core.Interfaces;

namespace BG3DiceSystem.Core.Services
{
    public class LocalizationService : ILocalizationService
    {
        public Language CurrentLanguage { get; private set; } = Language.EN;
        public event Action OnLanguageChanged;

        private readonly Dictionary<string, (string en, string ru)> _translations = new Dictionary<string, (string, string)>
        {
            // Top DC Banner
            { "dc_banner_header", ("DIFFICULTY CLASS", "КЛАСС СЛОЖНОСТИ") },
            { "dc_class_fmt", ("Difficulty Class (DC {0})", "Класс сложности (КС {0})") },
            { "target_dc_fmt", ("Target DC: {0}", "Целевой КС: {0}") },

            // Left Panel
            { "extra_settings_title", ("Extra Settings", "Доп. Настройки") },
            { "bonus_fmt", ("Bonus ({0})", "Бонус ({0})") },
            { "bonus_label", ("Bonus", "Бонус") },
            { "bonus_label_upper", ("BONUS", "БОНУС") },
            { "modifiers_count_fmt", ("Modifiers ({0})", "Модификаторы ({0})") },
            { "modifiers_list_fmt", ("MODIFIERS LIST ({0})", "СПИСОК МОДИФИКАТОРОВ ({0})") },

            // Roll Modes & Toggles
            { "mode_single_die", ("Single Die", "1 кубик") },
            { "mode_normal", ("Normal", "Обычный") },
            { "mode_advantage", ("Advantage", "Преимущество") },

            // Right Panel & Action
            { "current_ability_check_title", ("Current Ability Check", "Проверка способности") },
            { "selected_skill_header", ("Selected Skill", "Выбранный навык") },
            { "target_info_header", ("Target Information", "Информация о цели") },
            { "dice_result_label", ("Dice Result", "Результат кубика") },
            { "roll_button", ("ROLL", "БРОСОК") },
            { "quit_button", ("QUIT", "ВЫХОД") },

            // Modifiers & Presets
            { "add_modifier", ("+ Add Modifier", "+ Добавить бонус") },
            { "preset_guidance", ("Guidance (+2)", "Наставление (+2)") },
            { "preset_proficiency", ("Proficiency (+2)", "Владение (+2)") },
            { "preset_plus_one", ("Bonus (+1)", "Бонус (+1)") },

            // Sub-view Tab Buttons
            { "tab_history", ("History", "История") },
            { "tab_autotests", ("Auto Tests", "Авто-тесты") },

            // Roll Outcomes
            { "outcome_success", ("SUCCESS", "УСПЕХ") },
            { "outcome_failure", ("FAILURE", "НЕУДАЧА") },
            { "outcome_crit_success", ("CRITICAL SUCCESS!", "КРИТИЧЕСКИЙ УСПЕХ!") },
            { "outcome_crit_failure", ("CRITICAL FAILURE!", "КРИТИЧЕСКИЙ ПРОВАЛ!") },

            // History Panel
            { "history_success", ("SUCCESS", "УСПЕХ") },
            { "history_failure", ("FAILURE", "НЕУДАЧА") },
            { "history_crit_success", ("CRIT SUCCESS", "КРИТ. УСПЕХ") },
            { "history_crit_failure", ("CRIT FAIL", "КРИТ. ПРОВАЛ") },
            { "history_clear", ("Clear History", "Очистить историю") },

            // Auto Play Test View
            { "autotest_title", ("AUTOMATED TEST SUITE", "АВТОМАТИЧЕСКИЕ ТЕСТЫ") },
            { "autotest_delay_fmt", ("Delay: {0:F1}s", "Задержка: {0:F1}с") },
            { "autotest_run", ("Run Tests", "Запустить тесты") },
            { "autotest_stop", ("Stop", "Стоп") },
            { "autotest_close", ("Close", "Закрыть") },
            { "autotest_initializing", ("Initializing Automated Test Suite...", "Инициализация авто-тестов...") },
            { "autotest_running_fmt", ("Running [{0}/{1}]: {2} ({3})", "Выполнение [{0}/{1}]: {2} ({3})") },
            { "autotest_summary_fmt", ("Total: {0} | Passed: {1} | Failed: {2} | Duration: {3:F1}s", "Всего: {0} | Успешно: {1} | Ошибок: {2} | Время: {3:F1}с") },
            { "autotest_copy", ("Copy Report", "Скопировать отчет") },
            { "autotest_pass", ("PASS ✓", "УСПЕХ ✓") },
            { "autotest_fail", ("FAIL ✗", "ОШИБКА ✗") },
        };

        private readonly Dictionary<string, (string enName, string ruName, string enDesc, string ruDesc)> _skills = new Dictionary<string, (string, string, string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            { "Persuasion", (
                "Persuasion", "Убеждение",
                "Attempt to convince a creature through diplomacy and tact.",
                "Попытка убедить существо с помощью дипломатии и такта."
            ) },
            { "Athletics", (
                "Athletics", "Атлетика",
                "Perform physical feats such as climbing, jumping, or swimming.",
                "Выполнение физических трюков, таких как лазание, прыжки или плавание."
            ) },
            { "Stealth", (
                "Stealth", "Скрытность",
                "Conceal yourself from enemies and slip past unnoticed.",
                "Способность оставаться незамеченным и незаметно пробираться мимо врагов."
            ) },
            { "Arcana", (
                "Arcana", "Магия",
                "Recall lore about spells, magic items, and magical planes.",
                "Знания о заклинаниях, магических предметах и потусторонних планах."
            ) },
            { "Lockpicking", (
                "Lockpicking", "Взлом замков",
                "Use thieves' tools to pick locks and disable traps.",
                "Использование воровских инструментов для взлома замков и отключения ловушек."
            ) }
        };

        private readonly Dictionary<string, (string en, string ru)> _modifiers = new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            { "Athletics", ("Athletics", "Атлетика") },
            { "Wisdom", ("Wisdom", "Мудрость") },
            { "Proficiency", ("Proficiency", "Владение") },
            { "Guidance", ("Guidance", "Наставление") },
            { "Bless", ("Bless", "Благословение") },
            { "Bonus", ("Bonus", "Бонус") }
        };

        public void SetLanguage(Language language)
        {
            if (CurrentLanguage == language) return;
            CurrentLanguage = language;
            Debug.Log($"[LocalizationService] Language switched to: {language}");
            OnLanguageChanged?.Invoke();
        }

        public string GetText(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            if (_translations.TryGetValue(key, out var val))
            {
                return CurrentLanguage == Language.RU ? val.ru : val.en;
            }
            return key;
        }

        public string GetText(string key, params object[] args)
        {
            string fmt = GetText(key);
            try
            {
                return string.Format(fmt, args);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LocalizationService] Error formatting key '{key}': {ex.Message}");
                return fmt;
            }
        }

        public string GetSkillName(string englishSkillName)
        {
            if (string.IsNullOrEmpty(englishSkillName)) return string.Empty;
            if (_skills.TryGetValue(englishSkillName, out var s))
            {
                return CurrentLanguage == Language.RU ? s.ruName : s.enName;
            }
            return englishSkillName;
        }

        public string GetSkillDescription(string englishSkillName)
        {
            if (string.IsNullOrEmpty(englishSkillName)) return string.Empty;
            if (_skills.TryGetValue(englishSkillName, out var s))
            {
                return CurrentLanguage == Language.RU ? s.ruDesc : s.enDesc;
            }
            return string.Empty;
        }

        public string GetModifierName(string englishModifierName)
        {
            if (string.IsNullOrEmpty(englishModifierName)) return string.Empty;
            if (_modifiers.TryGetValue(englishModifierName, out var m))
            {
                return CurrentLanguage == Language.RU ? m.ru : m.en;
            }
            return englishModifierName;
        }
    }
}
