// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared.Shuttles.Events;

namespace Content.Server.DeadSpace.ThrustersConfig;

public sealed class ThrustersConfigSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ThrusterSystem _thrusterSystem = default!;

    private readonly HashSet<Entity<ThrusterComponent>> _thrustersSet = new();
    private const int MaxGyroscopeThrust = 2000;
    private const int MaxThrusterThrust = 100;

    public override void Initialize()
    {
        SubscribeLocalEvent<ShuttleConsoleComponent, ThrustersRestartMessage>(OnThrustersRestartMessage);
    }

    private void OnThrustersRestartMessage(EntityUid ent, ShuttleConsoleComponent comp, ThrustersRestartMessage msg)
    {
        if (msg.ThrustersThrust > MaxThrusterThrust || msg.GyroscopeThrust > MaxGyroscopeThrust) return;

        var shuttleEntity = GetEntity(msg.ShuttleEntity);

        _thrustersSet.Clear();
        _lookup.GetChildEntities(shuttleEntity, _thrustersSet);
        foreach (var thruster in _thrustersSet)
        {
            var thrusterComponent = Comp<ThrusterComponent>(thruster);
            _thrusterSystem.DisableThruster(thruster, thrusterComponent);
            switch (thrusterComponent.Type)
            {
                case ThrusterType.Angular:
                    thrusterComponent.Thrust = msg.GyroscopeThrust;
                    break;
                case ThrusterType.Linear:
                    thrusterComponent.Thrust = msg.ThrustersThrust;
                    break;
            }
            _thrusterSystem.EnableThruster(thruster, thrusterComponent);
        }
    }
}
