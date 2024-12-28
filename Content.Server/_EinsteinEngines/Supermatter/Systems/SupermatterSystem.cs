using Content.Server.AlertLevel;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Server.DoAfter;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Kitchen.Components;
using Content.Server.Lightning;
using Content.Server.Popups;
using Content.Server.Station.Systems;
using Content.Shared._EinsteinEngines.Supermatter.Components;
using Content.Shared.Atmos;
using Content.Shared.Audio;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Projectiles;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Events;

namespace Content.Server._EinsteinEngines.Supermatter.Systems;

public sealed partial class SupermatterSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly TransformSystem _xform = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambient = default!;
    [Dependency] private readonly LightningSystem _lightning = default!;
    [Dependency] private readonly AlertLevelSystem _alert = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SupermatterComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<SupermatterComponent, StartCollideEvent>(OnCollideEvent);
        SubscribeLocalEvent<SupermatterComponent, InteractHandEvent>(OnHandInteract);
        SubscribeLocalEvent<SupermatterComponent, InteractUsingEvent>(OnItemInteract);
        SubscribeLocalEvent<SupermatterComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<SupermatterComponent, SupermatterDoAfterEvent>(OnGetSliver);
    }


    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var sm in EntityManager.EntityQuery<SupermatterComponent>())
        {
            if (!sm.Activated)
                return;

            var uid = sm.Owner;
            sm.UpdateAccumulator += frameTime;

            if (sm.UpdateAccumulator >= sm.UpdateTimer)
            {
                sm.UpdateAccumulator -= sm.UpdateTimer;
                Cycle(uid, sm);
            }
        }
    }


    public void Cycle(EntityUid uid, SupermatterComponent sm)
    {
        sm.ZapAccumulator++;
        sm.YellAccumulator++;

        ProcessAtmos(uid, sm);
        HandleDamage(uid, sm);

        if (sm.Damage >= sm.DamageDelaminationPoint || sm.Delamming)
            HandleDelamination(uid, sm);

        HandleSoundLoop(uid, sm);

        if (sm.ZapAccumulator >= sm.ZapTimer)
        {
            sm.ZapAccumulator -= sm.ZapTimer;
            SupermatterZap(uid, sm);
        }

        if (sm.YellAccumulator >= sm.YellTimer)
        {
            sm.YellAccumulator -= sm.YellTimer;
            AnnounceCoreDamage(uid, sm);
        }
    }

    private void OnMapInit(EntityUid uid, SupermatterComponent sm, MapInitEvent args)
    {
        // Set the Sound
        _ambient.SetAmbience(uid, true);

        // Add Air to the initialized SM in the Map so it doesn't delam on its' own
        var mix = _atmosphere.GetContainingMixture(uid, true, true);
        mix?.AdjustMoles(Gas.Oxygen, Atmospherics.OxygenMolesStandard);
        mix?.AdjustMoles(Gas.Nitrogen, Atmospherics.NitrogenMolesStandard);
    }

    private void OnCollideEvent(EntityUid uid, SupermatterComponent sm, ref StartCollideEvent args)
    {
        if (!sm.Activated)
            sm.Activated = true;

        var target = args.OtherEntity;
        if (args.OtherBody.BodyType == BodyType.Static
            || HasComp<SupermatterImmuneComponent>(target)
            || _container.IsEntityInContainer(uid))
            return;

        if (!HasComp<ProjectileComponent>(target))
        {
            EntityManager.SpawnEntity(sm.CollisionResultPrototype, Transform(target).Coordinates);
            _audio.PlayPvs(sm.DustSound, uid);
            sm.Power += args.OtherBody.Mass;
        }

        EntityManager.QueueDeleteEntity(target);

        if (TryComp<SupermatterFoodComponent>(target, out var food))
            sm.Power += food.Energy;
        else if (TryComp<ProjectileComponent>(target, out var projectile))
            sm.Power += (float) projectile.Damage.GetTotal();
        else
            sm.Power++;

        sm.MatterPower += HasComp<MobStateComponent>(target) ? 200 : 0;
    }

    private void OnHandInteract(EntityUid uid, SupermatterComponent sm, ref InteractHandEvent args)
    {
        if (!sm.Activated)
            sm.Activated = true;

        var target = args.User;

        if (HasComp<SupermatterImmuneComponent>(target))
            return;

        sm.MatterPower += 200;

        EntityManager.SpawnEntity(sm.CollisionResultPrototype, Transform(target).Coordinates);
        _audio.PlayPvs(sm.DustSound, uid);
        EntityManager.QueueDeleteEntity(target);
    }

    private void OnItemInteract(EntityUid uid, SupermatterComponent sm, ref InteractUsingEvent args)
    {
        if (!sm.Activated)
            sm.Activated = true;

        if (sm.SliverRemoved)
            return;

        if (!HasComp<SharpComponent>(args.Used))
            return;

        var dae = new DoAfterArgs(EntityManager, args.User, 30f, new SupermatterDoAfterEvent(), args.Target)
        {
            BreakOnDamage = true,
            BreakOnHandChange = false,
            BreakOnWeightlessMove = false,
            NeedHand = true,
            RequireCanInteract = true,
        };

        _doAfter.TryStartDoAfter(dae);
        _popup.PopupClient(Loc.GetString("supermatter-tamper-begin"), uid, args.User);
    }

    private void OnGetSliver(EntityUid uid, SupermatterComponent sm, ref SupermatterDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        // Your criminal actions will not go unnoticed
        sm.Damage += sm.DamageDelaminationPoint / 10;

        var integrity = GetIntegrity(sm).ToString("0.00");
        SendSupermatterAnnouncement(uid, Loc.GetString("supermatter-announcement-cc-tamper", ("integrity", integrity)), true, "Central Command");

        Spawn(sm.SliverPrototype, _transform.GetMapCoordinates(args.User));
        _popup.PopupClient(Loc.GetString("supermatter-tamper-end"), uid, args.User);

        sm.DelamTimer /= 2;
    }

    private void OnExamine(EntityUid uid, SupermatterComponent sm, ref ExaminedEvent args)
    {
        if (args.IsInDetailsRange)
            args.PushMarkup(Loc.GetString("supermatter-examine-integrity", ("integrity", GetIntegrity(sm).ToString("0.00"))));
    }
}
