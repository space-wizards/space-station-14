using System;
using Content.Shared;
using Content.Shared.Module;

namespace Content.Client.IoC
{
    public sealed partial class ClientModuleTestingCallbacks : SharedModuleTestingCallbacks
    {
        public Action? ClientBeforeIoC { get; set; }
    }
}

