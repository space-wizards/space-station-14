using System;

namespace Content.Server.Cargo
{
    public class CargoBankAccount : ICargoBankAccount
    {
        public int Id { get; }

        public string Name { get; }

        public int Balance { get; set; }

        public CargoBankAccount(int id, string name, int balance)
        {
            Id = id;
            Name = name;
            Balance = balance;
        }
    }
}
