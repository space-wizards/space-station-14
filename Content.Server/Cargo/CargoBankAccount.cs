using System;
using Robust.Shared.ViewVariables;

namespace Content.Server.Cargo
{
    public sealed class CargoBankAccount : ICargoBankAccount
    {
        [ViewVariables]
        public int Id { get; }
        [ViewVariables]
        public string Name { get; }

        private int _balance;
        [ViewVariables(VVAccess.ReadWrite)]
        public int Balance
        {
            get => _balance;
            set
            {
                if (_balance == value)
                    return;
                _balance = value;
                OnBalanceChange?.Invoke();
            }
        }

        public event Action? OnBalanceChange;

        public CargoBankAccount(int id, string name, int balance)
        {
            Id = id;
            Name = name;
            Balance = balance;
        }
    }
}
