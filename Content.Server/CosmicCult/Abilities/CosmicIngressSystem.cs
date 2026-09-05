using Content.Server.Doors.Systems;
using Content.Shared.CosmicCult;
using Content.Shared.CosmicCult.Components.Actions;
using Content.Shared.Doors.Components;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Robust.Shared.Audio.Systems;

namespace Content.Server.CosmicCult.Abilities;

public sealed partial class CosmicIngressSystem : EntitySystem
{
    [Dependency] private DoorSystem _door = default!;
    [Dependency] private WeldableSystem _weld = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    [SubscribeLocalEvent]
    private void OnCosmicIngress(Entity<CosmicActionIngressComponent> ent, ref EventCosmicIngress args)
    {
        if (!TryComp<CosmicCultActionComponent>(ent, out var action))
            return;

        var target = args.Target;
        if (args.Handled)
            return;

        if (action.Empowered)
        {
            if (TryComp<DoorBoltComponent>(target, out var doorBolt))
                _door.SetBoltsDown((target, doorBolt), false);

            if (HasComp<WeldableComponent>(target))
                _weld.SetWeldedState(target, false);
        }

        // TODO: Predicted opening when this is moved to shared.
        if (_door.TryOpen(target, user: args.Performer, checkAccess: false))
        {
            args.Handled = true;
            _audio.PlayPvs(action.Sfx, ent);
            Spawn(action.Vfx, Transform(target).Coordinates);
        }
    }
}
