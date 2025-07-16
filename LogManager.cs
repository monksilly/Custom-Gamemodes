using BepInEx.Logging;

namespace CustomGameModes;

public static class LogManager
{
    private static ManualLogSource _logSource;

    public static void Init(ManualLogSource logSource)
    {
        _logSource = logSource;
    }
    
    public static void Info(object msg) => _logSource.LogInfo(msg);
    public static void Warn(object msg) => _logSource.LogWarning(msg);
    public static void Error(object msg) => _logSource.LogError(msg);
    public static void Debug(object msg) => _logSource.LogDebug(msg);
}