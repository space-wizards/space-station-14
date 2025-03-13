using Content.Shared.DoAfter;
using Content.Server.Popups;
using Content.Server._Impstation.CosmicCult.Components;
using Content.Shared.Interaction;
using Content.Shared._Impstation.CosmicCult.Components;
using Content.Shared._Impstation.CosmicCult;
using Content.Server.Atmos.Components;
using Content.Server.Body.Components;
using Content.Server.Bible.Components;
using Robust.Shared.Audio.Systems;
using Content.Shared.Temperature.Components;

namespace Content.Server._Impstation.CosmicCult.EntitySystems;
public sealed class CosmicRiftSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CosmicMalignRiftComponent, InteractHandEvent>(OnInteract);
        SubscribeLocalEvent<CosmicMalignRiftComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<CosmicCultComponent, EventAbsorbRiftDoAfter>(OnAbsorbDoAfter);
        SubscribeLocalEvent<CosmicMalignRiftComponent, EventPurgeRiftDoAfter>(OnPurgeDoAfter);
    }

    private void OnInteract(Entity<CosmicMalignRiftComponent> uid, ref InteractHandEvent args)
    {
        if (args.Handled || uid.Comp.Occupied)
        {
            _popup.PopupEntity(Loc.GetString("cosmiccult-rift-inuse"), args.User, args.User);
            return;
        }
        if (HasComp<BibleUserComponent>(args.User))
        {
            _popup.PopupEntity(Loc.GetString("cosmiccult-rift-chaplainoops"), args.User, args.User);
            return;
        }
        if (!TryComp<CosmicCultComponent>(args.User, out var cultist))
        {
            _popup.PopupEntity(Loc.GetString("cosmiccult-rift-invaliduser"), args.User, args.User);
            return;
        }
        if (cultist.CosmicEmpowered)
        {
            _popup.PopupEntity(Loc.GetString("cosmiccult-rift-alreadyempowered"), args.User, args.User);
            return;
        }
        args.Handled = true;
        uid.Comp.Occupied = true;
        _popup.PopupEntity(Loc.GetString("cosmiccult-rift-beginabsorb"), args.User, args.User);
        var doargs = new DoAfterArgs(EntityManager, args.User, uid.Comp.AbsorbTime, new EventAbsorbRiftDoAfter(), args.User, uid)
        {
            DistanceThreshold = 1.5f, Hidden = true, BreakOnDamage = true, BreakOnHandChange = true, BreakOnMove = true, MovementThreshold = 0.5f,
        };
        _doAfter.TryStartDoAfter(doargs);
    }

    private void OnInteractUsing(Entity<CosmicMalignRiftComponent> uid, ref InteractUsingEvent args)
    {
        if (args.Handled || uid.Comp.Occupied)
        {
            _popup.PopupEntity(Loc.GetString("cosmiccult-rift-inuse"), args.User, args.User);
            return;
        }
        if (HasComp<BibleComponent>(args.Used))
        {
            uid.Comp.Occupied = true;
            _popup.PopupEntity(Loc.GetString("cosmiccult-rift-beginpurge"), args.User, args.User);
            var doargs = new DoAfterArgs(EntityManager, args.User, uid.Comp.BibleTime, new EventPurgeRiftDoAfter(), uid, uid)
            {
                DistanceThreshold = 1.5f, Hidden = false, BreakOnDamage = true, BreakOnDropItem = true, BreakOnMove = true, MovementThreshold = 2f,
            };
            _doAfter.TryStartDoAfter(doargs);
        }
        else if (TryComp<CleanseOnUseComponent>(args.Used, out var comp) && comp.CanPurge == true)
        {
            uid.Comp.Occupied = true;
            _popup.PopupEntity(Loc.GetString("cosmiccult-rift-beginpurge"), args.User, args.User);
            var doargs = new DoAfterArgs(EntityManager, args.User, uid.Comp.ChaplainTime, new EventPurgeRiftDoAfter(), uid, uid)
            {
                DistanceThreshold = 1.5f, Hidden = false, BreakOnDamage = true, BreakOnDropItem = true, BreakOnMove = true, MovementThreshold = 2f,
            };
            _doAfter.TryStartDoAfter(doargs);
        }
    }

    private void OnAbsorbDoAfter(Entity<CosmicCultComponent> uid, ref EventAbsorbRiftDoAfter args)
    {
        var comp = uid.Comp;
        if (args.Args.Target == null || args.Cancelled || args.Handled)
        {
            if (TryComp<CosmicMalignRiftComponent>(args.Args.Target, out var rift))
                rift.Occupied = false;
            return;
        }
        args.Handled = true;
        var tgtpos = Transform(args.Args.Target.Value).Coordinates;
        var target = args.Args.Target.Value;
        Spawn(uid.Comp.AbsorbVFX, tgtpos);
        comp.CosmicEmpowered = true;
        comp.CosmicSiphonQuantity = 2;
        comp.CosmicGlareRange = 10;
        comp.CosmicGlareDuration = 10 * 1000f;
        comp.CosmicGlareStun = TimeSpan.FromSeconds(1);
        comp.CosmicImpositionDuration = TimeSpan.FromSeconds(7.2);
        comp.Respiration = false;
        EnsureComp<PressureImmunityComponent>(args.User);
        EnsureComp<TemperatureImmunityComponent>(args.User);
        _popup.PopupEntity(Loc.GetString("cosmiccult-rift-absorb"), uid, uid);
        QueueDel(target);
    }

    private void OnPurgeDoAfter(Entity<CosmicMalignRiftComponent> uid, ref EventPurgeRiftDoAfter args)
    {
        if (args.Args.Target == null || args.Cancelled || args.Handled)
        {
            uid.Comp.Occupied = false;
            return;
        }
        args.Handled = true;
        var tgtpos = Transform(uid).Coordinates;
        Spawn(uid.Comp.PurgeVFX, tgtpos);
        _audio.PlayPvs(uid.Comp.PurgeSound, args.User);
        _popup.PopupEntity(Loc.GetString("cosmiccult-rift-purge"), args.User, args.User);
        QueueDel(uid);
    }

}
