using Content.Shared.CosmicCult.Components.Actions;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Shared.CosmicCult.Abilities;

public sealed partial class CosmicIngressSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private CosmicCultSystem _cult = default!;
    [Dependency] private SharedDoorSystem _door = default!;
    [Dependency] private WeldableSystem _weld = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    [SubscribeLocalEvent]
    private void OnCosmicIngress(Entity<CosmicActionIngressComponent> ent, ref EventCosmicIngress args)
    {
        if (!_cult.CultActionQuery.TryComp(ent, out var action))
            return;

        var target = args.Target;
        if (args.Handled)
            return;

        if (action.Empowered || ent.Comp.AlwaysOpen)
        {
            if (TryComp<DoorBoltComponent>(target, out var doorBolt))
                _door.SetBoltsDown((target, doorBolt), false);

            if (HasComp<WeldableComponent>(target))
                _weld.SetWeldedState(target, false);
        }

        if (_door.TryOpen(target, user: args.Performer, checkAccess: false, predicted: true))
        {
            args.Handled = true;

            if (_net.IsServer)
            {
                _audio.PlayPvs(action.Sfx, target);
                Spawn(action.Vfx, Transform(target).Coordinates);
            }
        }
    }
}
