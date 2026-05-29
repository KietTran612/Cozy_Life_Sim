using UnityEngine;

namespace CozyLifeSim.Editor
{
    internal static class CozyValidationLog
    {
        public static void Pass(string scope, string message)
        {
            Debug.Log($"<color=green>[{scope}] PASS</color> {message}");
        }

        public static void Warn(string scope, string message)
        {
            Debug.LogWarning($"<color=yellow>[{scope}] WARN</color> {message}");
        }

        public static void ExpectedWarning(string scope, string message)
        {
            Debug.Log($"<color=yellow>[{scope}] EXPECTED WARNING</color> {message}");
        }

        public static void Fail(string scope, string message)
        {
            Debug.LogError($"<color=red>[{scope}] FAIL</color> {message}");
        }

        public static void Summary(string scope, int passed, int failed, int expectedWarnings = 0)
        {
            string status = failed == 0 ? "PASSED" : "FAILED";
            string color = failed == 0 ? "green" : "red";
            string warningText = expectedWarnings > 0 ? $", {expectedWarnings} expected warnings" : string.Empty;
            Debug.Log(
                "\n===================================================================================\n" +
                $"<size=14><b>[{scope}] <color={color}>{status}</color></b></size>\n" +
                $"<b>Summary:</b> {passed} passed, {failed} failed{warningText}\n" +
                "===================================================================================\n");
        }
    }
}
