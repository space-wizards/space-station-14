#nullable enable
using System;
using Robust.Shared.ContentPack;

namespace Content.Shared
{
    public abstract class SharedModuleTestingCallbacks : ModuleTestingCallbacks
    {
        public Action SharedBeforeIoC { get; set; } = default!;
    }
}
