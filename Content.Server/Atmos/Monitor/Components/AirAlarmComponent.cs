using System;
using System.Linq;
using System.Collections.Generic;
using Content.Server.Atmos.Monitor;
using Content.Server.Atmos.Monitor.Systems;
using Content.Server.Power.Components;
using Content.Server.UserInterface;
using Content.Shared.Atmos;
using Content.Shared.Interaction;
using Content.Shared.Atmos.Monitor.Components;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos.Monitor.Components
{
    [RegisterComponent]
    public class AirAlarmComponent : Component
    {
        [ComponentDependency] public readonly ApcPowerReceiverComponent? DeviceRecvComponent = default!;
        [ComponentDependency] public readonly AtmosMonitorComponent? AtmosMonitorComponent = default!;

        [ViewVariables] public AirAlarmMode CurrentMode { get; set; }

        // Remember to null this afterwards.
        [ViewVariables] public IAirAlarmModeUpdate? CurrentModeUpdater { get; set; }

        public override string Name => "AirAlarm";

        public Dictionary<string, IAtmosDeviceData> DeviceData = new();

        public HashSet<NetUserId> ActivePlayers = new();
    }
}
