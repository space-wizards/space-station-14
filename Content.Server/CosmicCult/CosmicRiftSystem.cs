using Content.Server.Actions;
using Content.Server.Atmos.EntitySystems;
using Content.Server.CosmicCult.Abilities;
using Content.Server.CosmicCult.Components;
using Content.Server.Popups;
using Content.Shared.CosmicCult;
using Content.Shared.CosmicCult.Components;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Station.Components;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Server.CosmicCult;

public sealed partial class CosmicRiftSystem : EntitySystem
{
    [Dependency] private ActionsSystem _actions = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private PopupSystem _popup = default!;

    public static readonly EntProtoId PressureImmunityEffect = "StatusEffectPressureImmunity";
    public static readonly EntProtoId MalignRiftEntity = "CosmicMalignRift";

    [SubscribeLocalEvent]
    private void OnInteract(Entity<CosmicRiftComponent> uid, ref ActivateInWorldEvent args)
    {
        if (!TryComp<CosmicCultistComponent>(args.User, out var cultist) || args.Handled )
            return;

        if (uid.Comp.Occupied)
        {
            _popup.PopupEntity(Loc.GetString("cosmiccult-rift-inuse"), args.User, args.User);
            return;
        }

        if (cultist.WasEmpowered)
        {
            _popup.PopupEntity(Loc.GetString("cosmiccult-rift-cannotabsorb"), args.User, args.User);
            return;
        }

        args.Handled = true;
        uid.Comp.Occupied = true;
        _popup.PopupEntity(Loc.GetString("cosmiccult-rift-beginabsorb"), args.User, args.User);
        var doargs = new DoAfterArgs(EntityManager, args.User, uid.Comp.AbsorbTime, new EventAbsorbRiftDoAfter(), args.User, uid)
        {
            MovementThreshold = 0.5f, DistanceThreshold = 1.5f, Hidden = true, BreakOnDamage = true, BreakOnHandChange = true, BreakOnMove = true,
        };
        _doAfter.TryStartDoAfter(doargs);
    }

    [SubscribeLocalEvent]
    private void OnAbsorbDoAfter(Entity<CosmicCultistComponent> uid, ref EventAbsorbRiftDoAfter args)
    {
        var comp = uid.Comp;
        if (args.Args.Target is not { } target || args.Cancelled || args.Handled)
        {
            if (TryComp<CosmicRiftComponent>(args.Args.Target, out var rift))
                rift.Occupied = false;
            return;
        }
        args.Handled = true;

        _actions.AddAction(uid, uid.Comp.CosmicFragmentationAction);
        Spawn(CosmicCultSystem.GenericVfx, Transform(target).Coordinates);

        var ev = new CosmicCultistEmpowerChangedEvent(uid, true);
        RaiseLocalEvent(uid, ref ev);

        comp.WasEmpowered = true;
        _statusEffects.TrySetStatusEffectDuration(uid, PressureImmunityEffect);
        _popup.PopupCoordinates(Loc.GetString("cosmiccult-rift-absorb", ("NAME", Identity.Entity(args.Args.User, EntityManager))), Transform(args.Args.User).Coordinates, PopupType.MediumCaution);
        QueueDel(target);
    }

    public void SpawnRift(Entity<StationDataComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;
        // TODO: COSMIC CULT - SPAWNING A RIFT REQUIRES TryFindRandomTileOnStation TO BE EXPOSED OUTSIDE OF THE GAMERULESYSTEM.
        // if (TryFindRandomTileOnStation( out var _, out var _, out var _, out var coords)) { Spawn("CosmicMalignRift", coords); }
    }
}
