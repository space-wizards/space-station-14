using Content.Server.GameObjects.Components.Cargo;
using Robust.Shared.Timing;
using System.Collections.Generic;

namespace Content.Server.Cargo
{
    public interface IGalacticBankManager
    {
        IEnumerable<CargoBankAccount> GetAllBankAccounts();

        void Shutdown();
        void Update(FrameEventArgs frameEventArgs);

        void CreateBankAccount(string name, int balance);
        CargoBankAccount GetBankAccount(int id);
        void AddComponent(CargoConsoleComponent cargoConsoleComponent);
        bool TryGetBankAccount(int id, out CargoBankAccount account);
        bool ChangeBalance(int id, int n);
    }
}
