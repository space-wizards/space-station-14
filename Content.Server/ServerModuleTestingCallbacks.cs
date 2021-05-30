using System;
using Content.Shared;

namespace Content.Server
{
    public sealed class ServerModuleTestingCallbacks : SharedModuleTestingCallbacks
    {
        public Action? ServerBeforeIoC { get; set; }
    }
}
