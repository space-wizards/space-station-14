using System;
using System.Threading;
using System.Collections.Generic;
using Content.Server.Atmos.Monitor.Systems;
using Content.Shared.Atmos.Monitor.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos.Monitor.Components
{
    [RegisterComponent]
    public sealed class AirAlarmComponent : Component
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        private AirAlarmSystem? _airAlarmSystem;

        [ViewVariables] public AirAlarmMode CurrentMode { get; set; } = AirAlarmMode.Filtering;

        // Remember to null this afterwards.
        [ViewVariables] public IAirAlarmModeUpdate? CurrentModeUpdater { get; set; }

        public Dictionary<string, IAtmosDeviceData> DeviceData = new();

        public HashSet<NetUserId> ActivePlayers = new();

        public bool CanSync = true;
    }
}
