using System.Collections.Generic;
using Content.Server.Cargo;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    public class CargoConsoleSystem : EntitySystem
    {
        /// <summary>
        /// How much time to wait (in seconds) before increasing bank accounts balance.
        /// </summary>
        private const float Delay = 10f;
        /// <summary>
        /// How many points to give to every bank account every <see cref="Delay"/> seconds.
        /// </summary>
        private const int PointIncrease = 10;

        /// <summary>
        /// Keeps track of how much time has elapsed since last balance increase.
        /// </summary>
        private float _timer;
        /// <summary>
        /// Stores all bank accounts.
        /// </summary>
        private readonly Dictionary<int, CargoBankAccount> _accountsDict = new();
        /// <summary>
        /// Used to assign IDs to bank accounts. Incremental counter.
        /// </summary>
        private int _accountIndex = 0;
        /// <summary>
        /// Enumeration of all bank accounts.
        /// </summary>
        public IEnumerable<CargoBankAccount> BankAccounts => _accountsDict.Values;
        /// <summary>
        /// The station's bank account.
        /// </summary>
        public CargoBankAccount StationAccount => GetBankAccount(0);

        public override void Initialize()
        {
            CreateBankAccount("Orbital Monitor IV Station", 100000);
        }

        public override void Update(float frameTime)
        {
            _timer += frameTime;
            if (_timer < Delay)
            {
                return;
            }

            _timer -= Delay;
            foreach (var account in BankAccounts)
            {
                account.Balance += PointIncrease;
            }
        }

        /// <summary>
        /// Creates a new bank account.
        /// </summary>
        public void CreateBankAccount(string name, int balance)
        {
            var account = new CargoBankAccount(_accountIndex, name, balance);
            _accountsDict.Add(_accountIndex, account);
            _accountIndex += 1;
        }

        /// <summary>
        /// Returns the bank account associated with the given ID.
        /// </summary>
        public CargoBankAccount GetBankAccount(int id)
        {
            return _accountsDict[id];
        }

        /// <summary>
        /// Returns whether the account exists, eventually passing the account in the out parameter.
        /// </summary>
        public bool TryGetBankAccount(int id, out CargoBankAccount account)
        {
            return _accountsDict.TryGetValue(id, out account);
        }

        /// <summary>
        /// Attempts to change the given account's balance.
        /// Returns false if there's no account associated with the given ID
        /// or if the balance would end up being negative.
        /// </summary>
        public bool ChangeBalance(int id, int amount)
        {
            if (!TryGetBankAccount(id, out var account))
            {
                return false;
            }

            if (account.Balance + amount < 0)
            {
                return false;
            }

            account.Balance += amount;
            return true;
        }
    }
}
