using Content.Server.Database;

namespace Content.Server.Interfaces
{
    public interface IDatabaseManager
    {
        void Initialize();
        IDatabaseConfiguration DbConfig { get; }
    }
}
