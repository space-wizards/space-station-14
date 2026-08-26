using Content.Server.Actions;
using Content.Server.CosmicCult.Components;
using Content.Server.Popups;
using Content.Shared.CosmicCult;
using Content.Shared.CosmicCult.Components;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;

namespace Content.Server.CosmicCult.EntitySystems;

public sealed class CosmicRiftSystem : EntitySystem
{
    [Dependency] private ActionsSystem _actions = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CosmicRiftComponent, ActivateInWorldEvent>(OnInteract);
        SubscribeLocalEvent<CosmicCultistComponent, EventAbsorbRiftDoAfter>(OnAbsorbDoAfter);
    }

    private void OnInteract(Entity<CosmicRiftComponent> uid, ref ActivateInWorldEvent args)
    {
        if (!TryComp<CosmicCultistComponent>(args.User, out var cultist) || args.Handled )
            return;

        if (uid.Comp.Occupied)
        {
            _popup.PopupEntity(Loc.GetString("cosmiccult-rift-inuse"), args.User, args.User);
            return;
        }

        if (cultist.CosmicEmpowered || cultist.WasEmpowered)
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
        var actionEnt = _actions.AddAction(uid, uid.Comp.CosmicFragmentationAction);
        Spawn(uid.Comp.GenericVfx, Transform(target).Coordinates);
        comp.ActionEntities.Add(actionEnt);
        comp.WasEmpowered = true;
        comp.CosmicEmpowered = true;
        comp.CosmicSiphonQuantity = 2;
        comp.CosmicGlareRange = 10;
        comp.CosmicShiftWindup = TimeSpan.FromSeconds(1);
        comp.CosmicGlareDuration = TimeSpan.FromSeconds(10);
        comp.CosmicGlareStun = TimeSpan.FromSeconds(1);
        comp.CosmicImpositionDuration = TimeSpan.FromSeconds(8);
        comp.CosmicShuntDuration = TimeSpan.FromSeconds(26);
        comp.CosmicShuntDelay = TimeSpan.FromSeconds(0.4);
        comp.Respiration = false;
        // TODO: COSMIC CULT - CULTISTS MUST BE PRESSURE IMMUNE
        _popup.PopupCoordinates(Loc.GetString("cosmiccult-rift-absorb", ("NAME", Identity.Entity(args.Args.User, EntityManager))), Transform(args.Args.User).Coordinates, PopupType.MediumCaution);
        QueueDel(target);
    }
}
