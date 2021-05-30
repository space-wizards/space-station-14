#nullable enable
namespace Content.Shared.Interfaces
{
    /// <summary>
    /// Provides a simple way to check whether calling code is being run by
    /// Robust.Client, or Robust.Server. Useful for code in Content.Shared
    /// that wants different behavior depending on if client or server is using it.
    /// </summary>
    public interface IModuleManager
    {
        /// <summary>
        /// Returns true if the code is being run by the client, returns false otherwise.
        /// </summary>
        bool IsClientModule { get; }
        /// <summary>
        /// Returns true if the code is being run by the server, returns false otherwise.
        /// </summary>
        bool IsServerModule { get; }
    }
}
