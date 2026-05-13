using Content.Shared.Module;

namespace Content.Server.IoC
{
    public sealed partial class ServerModuleTestingCallbacks : SharedModuleTestingCallbacks
    {
        public Action? ServerBeforeIoC { get; set; }
    }
}

