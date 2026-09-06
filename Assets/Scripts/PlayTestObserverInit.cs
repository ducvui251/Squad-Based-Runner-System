using System.Reflection;
using UnityEngine;

/// <summary>
/// TEMPORARY test harness — auto-starts the game in play mode so hazards
/// actually run. Deleted after verification; no gameplay behavior change.
/// </summary>
public static class PlayTestObserverInit
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoStartForTest()
    {
        // Slow the run so log capture is reliable.
        Time.timeScale = 0.7f;

        // Force the game active flag (auto-property backing field) and StartGame.
        System.Type uiType = typeof(UIManager);
        FieldInfo backing = uiType.GetField("<IsGameActive>k__BackingField",
            BindingFlags.Static | BindingFlags.NonPublic);
        if (backing != null) backing.SetValue(null, true);

        MethodInfo start = uiType.GetMethod("StartGame",
            BindingFlags.Instance | BindingFlags.NonPublic);
        UIManager ui = UIManager.Instance;
        if (start != null && ui != null) start.Invoke(ui, null);

        Debug.Log("TEST_OBSERVER: game auto-started, timeScale=" + Time.timeScale);
    }
}
