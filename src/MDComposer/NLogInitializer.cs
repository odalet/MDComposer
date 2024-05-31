//#define TEST_LOGGERS
using NLog;
using NLog.Config;
using NLog.Targets;

namespace Delta.MDComposer;

using static ConsoleOutputColor;

internal static class NLogInitializer
{
    private static ColoredConsoleTarget? target;

    public static void Execute()
    {
        target = new ColoredConsoleTarget("console")
        {
            Layout = "${pad:padding=-5:fixedlength=true:inner=${level:uppercase=true}} - ${message}${onexception:inner=${newline}${exception:format=tostring}}",
            UseDefaultRowHighlightingRules = false
        };

        target.RowHighlightingRules.Add(new("level == LogLevel.Trace", Yellow, NoChange));
        target.RowHighlightingRules.Add(new("level == LogLevel.Debug", Blue, NoChange));
        target.RowHighlightingRules.Add(new("level == LogLevel.Info", White, NoChange));
        target.RowHighlightingRules.Add(new("level == LogLevel.Warn", Magenta, NoChange));
        target.RowHighlightingRules.Add(new("level == LogLevel.Error", Red, NoChange));
        target.RowHighlightingRules.Add(new("level == LogLevel.Fatal", Red, White));

        UpdateVerbosity(false); // default is not verbose
    }

    public static void UpdateVerbosity(bool verboseEnabled)
    {
        if (target == null) return;

        var config = new LoggingConfiguration();
        config.AddRule(LogLevel.Trace, LogLevel.Info, new NullTarget(), "AnyLog", final: true); // We only want warnings and errors from AnyLog
        config.AddRule(verboseEnabled ? LogLevel.Trace : LogLevel.Info, LogLevel.Fatal, target, "*");
        LogManager.Configuration = config;
        LogManager.ReconfigExistingLoggers();

#if TEST_LOGGERS
        var logger = LogManager.GetCurrentClassLogger();
        logger.Fatal("Example Fatal message");
        logger.Error("Example Error message");
        logger.Warn("Example Warning message");
        logger.Info("Example Info message");
        logger.Debug("Example Debug message");
        logger.Trace("Example Trace message");

        try
        {
            static void throwException() => throw new System.ApplicationException("This is a test exception...");
            throwException();
        }
        catch (System.Exception ex)
        {
            logger.Error(ex, $"Example Error message with exception '{ex.Message}'");
        }
#endif
    }
}
