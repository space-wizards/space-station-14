using System;

namespace Content.Server.Cargo
{
    public class CargoBankAccount : ICargoBankAccount
    {
        public int Id { get; }

        public string Name { get; }

        private int _balance;
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
