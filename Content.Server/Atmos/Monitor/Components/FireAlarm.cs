using Content.Server.Atmos.Monitor.Systems;
using Content.Server.Power.Components;
using Content.Shared.Interaction;
using Content.Shared.Atmos.Monitor;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;

namespace Content.Server.Atmos.Monitor.Components
{
    [RegisterComponent]
    public class FireAlarmComponent : Component
    {
        public override string Name => "FireAlarm";
    }
}
