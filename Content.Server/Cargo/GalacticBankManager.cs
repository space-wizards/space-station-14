using Content.Server.GameObjects.Components.Cargo;
using Robust.Shared.Timing;
using System;
using System.Collections.Generic;

namespace Content.Server.Cargo
{
    public class GalacticBankManager : IGalacticBankManager
    {
        private readonly float DELAY = 10f;
        private readonly int POINT_INCREASE = 10;

        private int _index = 0;
        private float _timer = 10f;
        private readonly Dictionary<int, CargoBankAccount> _accounts = new Dictionary<int, CargoBankAccount>();
        private readonly List<CargoConsoleComponent> _components = new List<CargoConsoleComponent>();

        public GalacticBankManager()
        {
            CreateBankAccount("Orbital Monitor IV Station", 100000);
        }

        public IEnumerable<CargoBankAccount> GetAllBankAccounts()
        {
            return _accounts.Values;
        }

        public void Shutdown()
        {
            throw new System.NotImplementedException();
        }

        public void CreateBankAccount(string name, int balance = 0)
        {
            var account = new CargoBankAccount(_index, name, balance);
            _accounts.Add(_index, account);
            _index += 1;
        }

        public CargoBankAccount GetBankAccount(int id)
        {
            return _accounts[id];
        }

        public bool TryGetBankAccount(int id, out CargoBankAccount account)
        {
            if (_accounts.TryGetValue(id, out var _account))
            {
                account = _account;
                return true;
            }
            account = null;
            return false;
        }

        public void Update(FrameEventArgs frameEventArgs)
        {
            _timer += frameEventArgs.DeltaSeconds;
            if (_timer < DELAY)
                return;
            _timer -= DELAY;
            foreach (var account in GetAllBankAccounts())
            {
                account.Balance += POINT_INCREASE;
            }
            SyncComponents();
        }

        private void SyncComponents()
        {
            foreach (var component in _components)
            {
                var account = GetBankAccount(component.BankId);
                if (account == null)
                    continue;
                component.SetState(account.Id, account.Name, account.Balance);
            }
        }

        private void SyncComponentsWithId(int id)
        {
            var account = GetBankAccount(id);
            foreach (var component in _components)
            {
                if (component.BankId != id)
                    continue;
                component.SetState(account.Id, account.Name, account.Balance);
            }
        }

        public void AddComponent(CargoConsoleComponent component)
        {
            if (_components.Contains(component))
                return;
            _components.Add(component);
        }

        public bool ChangeBalance(int id, int n)
        {
            if (!TryGetBankAccount(id, out var account))
                return false;
            if (account.Balance + n < 0)
                return false;
            account.Balance += n;
            SyncComponentsWithId(account.Id);
            return true;
        }
    }
}
