namespace Content.Server.Sandbox
{
    public interface ISandboxManager
    {
        bool IsSandboxEnabled { get; set; }
        void Initialize();
    }
}
