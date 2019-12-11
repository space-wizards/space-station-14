using DbUp.Engine.Output;
using Robust.Shared.Log;

// TODO: Move this somewhere else once more things use database migrations
namespace Content.Server.Preferences
{
    /// <summary>
    /// Adapter for DbUp.
    /// </summary>
    public class DatabaseLogger : IUpgradeLog
    {
        public void WriteInformation(string format, params object[] args)
        {
            Logger.InfoS("db", format, args);
        }

        public void WriteError(string format, params object[] args)
        {
            Logger.ErrorS("db", format, args);
        }

        public void WriteWarning(string format, params object[] args)
        {
            Logger.WarningS("db", format, args);
        }
    }
}
