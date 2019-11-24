using Content.Shared.Interfaces;

namespace Content.Server.Utility
{
    /// <summary>
    /// Server implementation of IModuleManager.
    /// Provides simple way for shared code to check if it's being run by
    /// the client of the server.
    /// </summary>
    public class ServerModuleManager : IModuleManager
    {
        bool IModuleManager.IsClientModule => false;
        bool IModuleManager.IsServerModule => true;
    }
}
