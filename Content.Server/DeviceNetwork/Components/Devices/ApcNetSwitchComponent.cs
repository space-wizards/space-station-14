// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Content.Server.DeviceNetwork.Systems.Devices;

namespace Content.Server.DeviceNetwork.Components.Devices
{
    [RegisterComponent]
    [Access(typeof(ApcNetSwitchSystem))]
    public sealed partial class ApcNetSwitchComponent : Component
    {
        [ViewVariables] public bool State;
    }
}
