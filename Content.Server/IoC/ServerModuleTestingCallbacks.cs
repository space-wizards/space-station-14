using System;
using Content.Shared;

namespace Content.Server.IoC
{
    public sealed class ServerModuleTestingCallbacks : SharedModuleTestingCallbacks
    {
        public Action? ServerBeforeIoC { get; set; }
    }
}
