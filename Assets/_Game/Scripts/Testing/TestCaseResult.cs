using System;
using System.Collections.Generic;
using System.Text;
using BG3DiceSystem.Gameplay.Dice;
using BG3DiceSystem.Gameplay.Roll;

namespace BG3DiceSystem.Testing
{
    [Serializable]
    public class TestCaseResult
    {
        public string TestName;
        public string SuiteCategory;
        public DiceType DiceType;
        public RollMode RollMode;
        public string SkillName;
        public int DifficultyClass;
        public int DiceValueA;
        public int DiceValueB;
        public int SelectedDiceValue;
        public int Modifier;
        public int Total;
        public string OutcomeText;
        public bool IsSuccess;
        public bool IsCriticalSuccess;
        public bool IsCriticalFailure;
        public bool IsPassed;
        public string FailureReason;
        public float DurationSeconds;
        public List<string> AppliedModifiers = new List<string>();

        public TestCaseResult(string testName, string suiteCategory)
        {
            TestName = testName;
            SuiteCategory = suiteCategory;
            IsPassed = true;
            FailureReason = string.Empty;
        }

        public void Fail(string reason)
        {
            IsPassed = false;
            if (string.IsNullOrEmpty(FailureReason))
            {
                FailureReason = reason;
            }
            else
            {
                FailureReason += $" | {reason}";
            }
        }
    }

    [Serializable]
    public class TestReport
    {
        public string SuiteName;
        public DateTime ExecutionTime;
        public float TotalDurationSeconds;
        public List<TestCaseResult> Results = new List<TestCaseResult>();

        public int TotalTests => Results.Count;
        public int PassedCount => Results.FindAll(r => r.IsPassed).Count;
        public int FailedCount => Results.FindAll(r => !r.IsPassed).Count;
        public float PassPercentage => TotalTests > 0 ? (float)PassedCount / TotalTests * 100f : 0f;

        public TestReport(string suiteName = "BG3 Dice Automated Test Suite")
        {
            SuiteName = suiteName;
            ExecutionTime = DateTime.Now;
        }

        public string GenerateMarkdownReport()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"# {SuiteName} - Report");
            sb.AppendLine($"**Executed At**: {ExecutionTime:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"**Total Tests**: {TotalTests} | **Passed**: {PassedCount} | **Failed**: {FailedCount} | **Pass Rate**: {PassPercentage:F1}%");
            sb.AppendLine($"**Total Duration**: {TotalDurationSeconds:F2} seconds");
            sb.AppendLine();
            sb.AppendLine("## Test Checklist Results");
            sb.AppendLine("| Status | Category | Test Name | Dice | Mode | Dice Values | Mod | Total / DC | Outcome | Failure Details |");
            sb.AppendLine("| :---: | :--- | :--- | :---: | :---: | :---: | :---: | :---: | :---: | :--- |");

            foreach (var r in Results)
            {
                string statusBadge = r.IsPassed ? "✅ PASS" : "❌ FAIL";
                string diceValuesStr = (r.RollMode == RollMode.AdvantageTwoDice) 
                    ? $"[{r.DiceValueA}, {r.DiceValueB}] -> {r.SelectedDiceValue}" 
                    : $"{r.SelectedDiceValue}";

                sb.AppendLine($"| {statusBadge} | {r.SuiteCategory} | {r.TestName} | {r.DiceType} | {r.RollMode} | {diceValuesStr} | +{r.Modifier} | {r.Total} / {r.DifficultyClass} | {r.OutcomeText} | {(string.IsNullOrEmpty(r.FailureReason) ? "-" : r.FailureReason)} |");
            }

            return sb.ToString();
        }
    }
}
