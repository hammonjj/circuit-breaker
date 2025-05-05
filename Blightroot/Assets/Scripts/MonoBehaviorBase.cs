using UnityEngine;

/* Example Usage:

public class EnemyAI : MonoBehaviourBase
{
    protected override void Awake()
    {
        base.Awake();
        LogInfo("EnemyAI initialized.");
    }

    protected override void DrawGizmosSafe()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 1f);
    }
}

*/

public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error,
    None
}

public abstract class MonoBehaviourBase : MonoBehaviour
{
    [Header("Debug Settings")]
    [Tooltip("Enable to draw gizmos for this object.")]
    public bool showGizmos = false;

    [Tooltip("Minimum log level to output to the console.")]
    public LogLevel logLevel = LogLevel.Info;

    private string _cachedObjectName;
    private int _debugId;

    protected virtual void Awake()
    {
        _cachedObjectName = gameObject.name;
        _debugId = GetInstanceID();
    }

    protected void LogDebug(string message)
    {
        if (logLevel <= LogLevel.Debug)
            Debug.Log(FormatLog("DEBUG", message));
    }

    protected void LogInfo(string message)
    {
        if (logLevel <= LogLevel.Info)
            Debug.Log(FormatLog("INFO", message));
    }

    protected void LogWarning(string message)
    {
        if (logLevel <= LogLevel.Warning)
            Debug.LogWarning(FormatLog("WARNING", message));
    }

    protected void LogError(string message)
    {
        if (logLevel <= LogLevel.Error)
            Debug.LogError(FormatLog("ERROR", message));
    }

    private string FormatLog(string level, string message)
    {
        return $"[{Time.frameCount}] [{Time.time:0.00}s] [{level}] [{_cachedObjectName} #{_debugId}] {message}";
    }

    // Example usage for conditional gizmo drawing
    protected virtual void OnDrawGizmos()
    {
        if (!showGizmos) return;
        DrawGizmosSafe();
    }

    /// <summary>
    /// Override this to implement gizmo drawing that respects the showGizmos toggle.
    /// </summary>
    protected virtual void DrawGizmosSafe() { }
}
