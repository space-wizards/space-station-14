using Content.Shared.Actions;
using Content.Shared.Antag;
using Content.Shared.CosmicCult.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Doors.Systems;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Ghost.Components;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Movement.Components;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Stealth.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.CosmicCult;

public abstract class SharedCosmicCultSystem : EntitySystem
{
    [Dependency] protected IGameTiming Timing = default!;
    [Dependency] protected IPrototypeManager Prototype = default!;
    [Dependency] protected IRobustRandom Random = default!;
    [Dependency] protected ISharedPlayerManager PlayerManager = default!;
    [Dependency] protected SharedActionsSystem Actions = default!;
    [Dependency] protected SharedAudioSystem Audio = default!;
    [Dependency] protected SharedPopupSystem PopUp = default!;
    [Dependency] protected SharedTransformSystem TransformSystem = default!;

    [Dependency] private INetManager _net = default!;
    [Dependency] private NameModifierSystem _nameModifier = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedDoorSystem _door = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedPointLightSystem _light = default!;
    [Dependency] private SharedRoleSystem _role = default!;

    public static readonly EntProtoId StunId = "StatusEffectStunned";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CosmicCultistComponent, ComponentGetStateAttemptEvent>(OnCosmicCultCompGetStateAttempt);
        SubscribeLocalEvent<CosmicCultistComponent, ComponentStartup>(DirtyCosmicCultComps);

        SubscribeLocalEvent<CosmicExamineComponent, ExaminedEvent>(OnCosmicCultExamined);

        SubscribeLocalEvent<CosmicColliderComponent, PreventCollideEvent>(OnPreventCollide);
        SubscribeLocalEvent<CosmicEquipmentComponent, BeforeGettingEquippedHandEvent>(OnPickupAttempt);
        SubscribeLocalEvent<CosmicStarMarkComponent, BeforeDamageChangedEvent>(OnDamaged);
        SubscribeLocalEvent<CosmicStarMarkComponent, DamageModifyEvent>(OnDamageModified);
        SubscribeLocalEvent<CosmicStarMarkComponent, RefreshNameModifiersEvent>(OnRefreshNameModifiers);
        SubscribeLocalEvent<CosmicStigmaComponent, CosmicStigmaDoAfter>(OnStigmaDoAfter);
        SubscribeLocalEvent<CosmicStigmaComponent, InteractHandEvent>(OnStigmaInteracted);
        SubscribeLocalEvent<CosmicMonumentComponent, InteractHandEvent>(OnMonumentInteracted);
        SubscribeLocalEvent<CosmicDoorComponent, InteractHandEvent>(OnDoorInteracted);
        SubscribeLocalEvent<CosmicBreachComponent, InteractHandEvent>(OnBreachInteracted);
        SubscribeLocalEvent<CosmicFontComponent, InteractUsingEvent>(OnFontInteracted);
    }
    private void OnPreventCollide(EntityUid uid, CosmicColliderComponent comp, ref PreventCollideEvent args)
    {
        if (EntityIsCultist(args.OtherEntity))
            args.Cancelled = true;
    }

    private void OnDamaged(Entity<CosmicStarMarkComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (args.Cancelled)
            return;

        if (HasComp<CosmicCultistComponent>(args.Origin))
            args.Cancelled = true;
    }

    private void OnDamageModified(Entity<CosmicStarMarkComponent> ent, ref DamageModifyEvent args)
    {
        if (TryComp<CosmicCultistComponent>(ent, out var cultComp) && cultComp.AstralAegisStacks > 0 && Above5Damage(args.Damage))
        {
            if (_net.IsServer)
            {
                Spawn(cultComp.GenericVfx, Transform(ent).Coordinates);
                Audio.PlayEntity(cultComp.AegisDeflectSfx, ent, ent);
            }
            cultComp.AstralAegisStacks--;
            args.Damage = args.OriginalDamage * 0.5;
            Dirty(ent.Owner, cultComp);
        }
    }

    public bool Above5Damage(DamageSpecifier damage)
    {
        foreach (var value in damage.DamageDict.Values)
        {
            if (value > (FixedPoint2) 5)
                return true;
        }

        return false;
    }

    private void OnStigmaDoAfter(Entity<CosmicStigmaComponent> ent, ref CosmicStigmaDoAfter args)
    {
        if (args.Handled || args.Cancelled)
            return;

        Audio.PlayPredicted(ent.Comp.DestroySfx, Transform(ent).Coordinates, args.User);
        PredictedSpawnAtPosition(ent.Comp.GenericVfx, Transform(ent).Coordinates);
        PredictedQueueDel(ent);
        args.Handled = true;
    }

    private void OnStigmaInteracted(Entity<CosmicStigmaComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (!ent.Comp.Harvested && EntityIsCultist(args.User) && Timing.IsFirstTimePredicted)
        {
            var stigmaCrystal = PredictedSpawnAtPosition("CosmicCultStigmaCrystal", Transform(ent).Coordinates);
            var farFilter = Filter.Empty().AddInRange(TransformSystem.GetMapCoordinates(ent), 25f);
            ent.Comp.Harvested = true;
            _hands.TryPickupAnyHand(args.User, stigmaCrystal);
            if (_net.IsServer)
            {
                Audio.PlayGlobal(ent.Comp.HarvestSfx, farFilter, true);
                Brand(args.User);
            }

            PredictedSpawnAtPosition(ent.Comp.GenericVfx, Transform(ent).Coordinates);
            _appearance.SetData(ent, CosmicFontVisualLayers.Base, true);
            if (TryComp<CosmicExamineComponent>(ent, out var examine))
                examine.CultistText = "cosmic-examine-text-stigma-harvested";
        }
        else if (!EntityIsCultist(args.User))
        {
            var destroyTime = ent.Comp.Harvested ? ent.Comp.DestroyTime / 4 : ent.Comp.DestroyTime;
            var doargs = new DoAfterArgs(EntityManager, args.User, destroyTime, new CosmicStigmaDoAfter(), ent, ent)
            {
                DistanceThreshold = 1.5f, Hidden = false, BreakOnDamage = false, BreakOnMove = true, BreakOnDropItem = true, BreakOnHandChange = true,
            };
            _doAfter.TryStartDoAfter(doargs);
        }
        Dirty(ent);
        args.Handled = true;
    }

    protected virtual void OnMonumentInteracted(Entity<CosmicMonumentComponent> ent, ref InteractHandEvent args) { }

    private void OnDoorInteracted(Entity<CosmicDoorComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled || EntityIsCultist(args.User))
            return;

        _door.StartOpening(ent);
    }

    private void OnBreachInteracted(Entity<CosmicBreachComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled || EntityIsCultist(args.User) || !HasComp<HumanoidProfileComponent>(args.User) || ent.Comp.LinkedBreach is null)
            return;

        if (Timing.IsFirstTimePredicted)
        {
            TransformSystem.SetMapCoordinates(args.User, TransformSystem.GetMapCoordinates(ent.Comp.LinkedBreach.Value));
            Spawn(ent.Comp.TeleportVfx,  Transform(args.User).Coordinates);
            Spawn(ent.Comp.TeleportVfx,  Transform(ent.Comp.LinkedBreach.Value).Coordinates);
            Audio.PlayPvs(ent.Comp.TeleportSfx, Transform(ent).Coordinates);
        }
    }

    private void OnFontInteracted(Entity<CosmicFontComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || ent.Comp.Activated || !EntityIsCultist(args.User) || !HasComp<CosmicStigmaItemComponent>(args.Used))
            return;

        if (ent.Comp.FinaleRunning && Timing.IsFirstTimePredicted)
        {
            Audio.PlayPredicted(ent.Comp.InsertSfx, Transform(ent).Coordinates, args.User);
            PredictedSpawnAtPosition(ent.Comp.Plinth, Transform(ent).Coordinates);
            PredictedSpawnAtPosition(Random.Pick(ent.Comp.Armors), Transform(ent).Coordinates);
            PredictedSpawnAtPosition(Random.Pick(ent.Comp.Weapons), Transform(ent).Coordinates);
            PredictedSpawnAtPosition(ent.Comp.GenericVfx, Transform(ent).Coordinates);
            PredictedQueueDel(ent);
        }
        else if (Timing.IsFirstTimePredicted)
        {
            ent.Comp.Activated = true;
            PredictedSpawnAtPosition(ent.Comp.GenericVfx, Transform(ent).Coordinates);
            Audio.PlayPredicted(ent.Comp.InsertSfx, Transform(ent).Coordinates, args.User);
            _appearance.SetData(ent, CosmicFontVisualLayers.Base, true);
            _light.SetEnabled(ent, false);
            if (TryComp<CosmicExamineComponent>(ent, out var examine))
                examine.CultistText = "cosmic-examine-text-font-activated";
        }
        PredictedQueueDel(args.Used);
        Dirty(ent);
        args.Handled = true;
    }

    private void OnCosmicCultExamined(Entity<CosmicExamineComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString(EntitySeesCult(args.Examiner) ? ent.Comp.CultistText : ent.Comp.OthersText));
    }

    public bool EntityIsCultist(EntityUid user)
    {
        if (!_mind.TryGetMind(user, out var mind, out _))
            return false;

        return HasComp<CosmicCultistComponent>(user) || _role.MindHasRole<CosmicCultRoleComponent>(mind);
    }

    public bool EntitySeesCult(EntityUid user)
    {
        return EntityIsCultist(user) || HasComp<GhostComponent>(user);
    }

    private void OnPickupAttempt(Entity<CosmicEquipmentComponent> ent, ref BeforeGettingEquippedHandEvent args)
    {
        if (!EntityIsCultist(args.User))
        {
            args.Cancelled = true;
            if (_net.IsClient && Timing.IsFirstTimePredicted)
                PopUp.PopupEntity(Loc.GetString("cosmiccult-gear-pickup", ("ITEM", ent)), args.User, args.User, PopupType.MediumCaution);
        }
    }

    /// <summary>
    /// Determines if a Cosmic Cultist component should be sent to the client.
    /// </summary>
    private void OnCosmicCultCompGetStateAttempt(EntityUid uid, CosmicCultistComponent comp, ref ComponentGetStateAttemptEvent args)
    {
        args.Cancelled = !CanGetState(args.Player);
    }

    /// <summary>
    /// The criteria that determine whether a Cult Member component should be sent to a client.
    /// </summary>
    /// <param name="player">The Player the component will be sent to.</param>
    private bool CanGetState(ICommonSession? player)
    {
        //Apparently this can be null in replays so I am just returning true.
        if (player?.AttachedEntity is not { } uid)
            return true;

        if (EntitySeesCult(uid))
            return true;

        return HasComp<ShowAntagIconsComponent>(uid);
    }

    /// <summary>
    /// Dirties all the Cult components so they are sent to clients.
    ///
    /// We need to do this because if a Cult component was not earlier sent to a client and for example the client
    /// becomes a Cult then we need to send all the components to it. To my knowledge there is no way to do this on a
    /// per client basis so we are just dirtying all the components.
    /// </summary>
    private void DirtyCosmicCultComps<T>(EntityUid someUid, T someComp, ComponentStartup ev)
    {
        var cosmicCultComps = AllEntityQuery<CosmicCultistComponent>();
        while (cosmicCultComps.MoveNext(out var uid, out var comp))
        {
            Dirty(uid, comp);
        }
    }

    /// <summary>
    /// Brands a cultist.
    /// </summary>
    public void Brand(EntityUid cultist)
    {
        EnsureComp<FootstepModifierComponent>(cultist);
        EnsureComp<CosmicStarMarkComponent>(cultist);
        RemComp<StealthComponent>(cultist); // This isn't hide-and-seek.
        _nameModifier.RefreshNameModifiers(cultist);
    }

    private void OnRefreshNameModifiers(Entity<CosmicStarMarkComponent> ent, ref RefreshNameModifiersEvent args)
    {
        args.AddModifier("cosmiccult-player-ascendant");
    }
}
