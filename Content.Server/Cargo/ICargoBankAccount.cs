using System;

namespace Content.Server.Cargo
{
    public interface ICargoBankAccount
    {
        int Id { get; }
        string Name { get; }
        int Balance { get; }
    }
}
