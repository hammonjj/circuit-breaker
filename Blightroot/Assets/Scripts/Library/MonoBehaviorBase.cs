using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;

public enum LogLevel {
    Debug,
    Info,
    Warning,
    Error,
    None
}

public abstract class MonoBehaviourBase : MonoBehaviour {
    [Header("Debug Settings")]
    [Tooltip("Enable to draw gizmos for this object.")]
    public bool showGizmos = false;
    [Tooltip("Minimum log level to output to the console.")]
    public LogLevel logLevel = LogLevel.Info;

    private string _cachedObjectName;
    private int _debugId;

    protected virtual void Awake() {
        _cachedObjectName = gameObject.name;
        _debugId = GetInstanceID();
    }

    protected void LogDebug(string message,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0
    ) {
        if (logLevel <= LogLevel.Debug) {
            Debug.Log(FormatLog("DEBUG", message, filePath, lineNumber));
        }
    }

    protected void LogInfo(string message,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0
    ) {
        if (logLevel <= LogLevel.Info) {
            Debug.Log(FormatLog("INFO", message, filePath, lineNumber));
        }
    }

    protected void LogWarning(string message,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0
    ) {
        if (logLevel <= LogLevel.Warning) {
            Debug.LogWarning(FormatLog("WARNING", message, filePath, lineNumber));
        }
    }

    protected void LogError(string message,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0
    ) {
        if (logLevel <= LogLevel.Error) {
            Debug.LogError(FormatLog("ERROR", message, filePath, lineNumber));
        }
    }

    private string FormatLog(string level, string msg, string filePath, int line) {
        var fileName = Path.GetFileName(filePath);
        return $"{fileName}.cs ({line}): [{level}] [{_cachedObjectName}#{_debugId}] {msg}";
    }

    protected virtual void OnDrawGizmos() {
        if (!showGizmos) {
            return;
        }
        DrawGizmosSafe();
    }

    protected virtual void DrawGizmosSafe() { }
}
