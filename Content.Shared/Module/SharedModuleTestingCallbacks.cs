// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.ContentPack;

namespace Content.Shared.Module
{
    public abstract class SharedModuleTestingCallbacks : ModuleTestingCallbacks
    {
        public Action SharedBeforeIoC { get; set; } = default!;
    }
}
