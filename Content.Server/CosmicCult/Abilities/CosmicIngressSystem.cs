using Content.Server.Doors.Systems;
using Content.Shared.CosmicCult;
using Content.Shared.CosmicCult.Components;
using Content.Shared.DoAfter;
using Content.Shared.Doors.Components;
using Robust.Shared.Audio.Systems;

namespace Content.Server.CosmicCult.Abilities;

public sealed partial class CosmicIngressSystem : EntitySystem
{
    [Dependency] private DoorSystem _door = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;

    [SubscribeLocalEvent]
    private void OnCosmicIngress(Entity<CosmicCultistComponent> uid, ref EventCosmicIngress args)
    {
        var target = args.Target;
        if (args.Handled)
            return;

        args.Handled = true;
        if (uid.Comp.CosmicEmpowered && TryComp<DoorBoltComponent>(target, out var doorBolt))
            _door.SetBoltsDown((target, doorBolt), false);
        _door.StartOpening(target);
        _audio.PlayPvs(uid.Comp.IngressSfx, uid);
        Spawn(uid.Comp.GenericVfx, Transform(target).Coordinates);
    }

    [SubscribeLocalEvent]
    private void OnColossusIngress(Entity<CosmicColossusComponent> ent, ref EventCosmicColossusIngress args)
    {
        var doargs = new DoAfterArgs(EntityManager, ent, ent.Comp.IngressDoAfter, new EventCosmicColossusIngressDoAfter(), ent, args.Target)
        {
            DistanceThreshold = 2f,
            Hidden = false,
            BreakOnMove = true,
        };
        args.Handled = true;
        _audio.PlayPvs(ent.Comp.DoAfterSfx, ent);
        _doAfter.TryStartDoAfter(doargs);
    }

    [SubscribeLocalEvent]
    private void OnColossusIngressDoAfter(Entity<CosmicColossusComponent> ent, ref EventCosmicColossusIngressDoAfter args)
    {
        if (args.Args.Target is not { } target)
            return;
        if (args.Cancelled || args.Handled)
            return;
        args.Handled = true;
        var comp = ent.Comp;

        if (TryComp<DoorBoltComponent>(target, out var doorBolt))
            _door.SetBoltsDown((target, doorBolt), false);
        _door.StartOpening(target);
        _audio.PlayPvs(comp.IngressSfx, ent);
        Spawn(comp.CultVfx, Transform(target).Coordinates);
    }
}
