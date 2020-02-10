using System;

namespace Content.Client.Sandbox
{
    public interface ISandboxManager
    {
        void Initialize();
        bool SandboxAllowed { get; }
        event Action<bool> AllowedChanged;
    }
}
