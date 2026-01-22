using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Examine;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Flash.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Light;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Content.Shared.Timing;
using Content.Shared.Traits.Assorted;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Linq;
using Content.Shared.Movement.Systems;
using Content.Shared.Random.Helpers;
using Content.Shared.Clothing.Components;

namespace Content.Shared.Flash;

public abstract class SharedFlashSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedChargesSystem _sharedCharges = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly MovementModStatusSystem _movementMod = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    private EntityQuery<StatusEffectsComponent> _statusEffectsQuery;
    private EntityQuery<DamagedByFlashingComponent> _damagedByFlashingQuery;
    private HashSet<EntityUid> _entSet = new();

    // The tag to add when a flash has no charges left.
    private static readonly ProtoId<TagPrototype> TrashTag = "Trash";
    // The key string for the status effect.
    public ProtoId<StatusEffectPrototype> FlashedKey = "Flashed";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FlashComponent, MeleeHitEvent>(OnFlashMeleeHit);
        SubscribeLocalEvent<FlashComponent, UseInHandEvent>(OnFlashUseInHand);
        SubscribeLocalEvent<FlashComponent, LightToggleEvent>(OnLightToggle);
        SubscribeLocalEvent<PermanentBlindnessComponent, FlashAttemptEvent>(OnPermanentBlindnessFlashAttempt);
        SubscribeLocalEvent<TemporaryBlindnessComponent, FlashAttemptEvent>(OnTemporaryBlindnessFlashAttempt);
        Subs.SubscribeWithRelay<FlashImmunityComponent, FlashAttemptEvent>(OnFlashImmunityFlashAttempt, held: false);
        SubscribeLocalEvent<FlashImmunityComponent, ExaminedEvent>(OnExamine);

        _statusEffectsQuery = GetEntityQuery<StatusEffectsComponent>();
        _damagedByFlashingQuery = GetEntityQuery<DamagedByFlashingComponent>();
    }

    private void OnFlashMeleeHit(Entity<FlashComponent> ent, ref MeleeHitEvent args)
    {
        if (!ent.Comp.FlashOnMelee ||
            !args.IsHit ||
            !args.HitEntities.Any() ||
            !UseFlash(ent, args.User))
        {
            return;
        }

        args.Handled = true;
        foreach (var target in args.HitEntities)
        {
            Flash(target, args.User, ent.Owner, ent.Comp.MeleeDuration, ent.Comp.SlowTo, melee: true, stunDuration: ent.Comp.MeleeStunDuration);
        }
    }

    private void OnFlashUseInHand(Entity<FlashComponent> ent, ref UseInHandEvent args)
    {
        if (!ent.Comp.FlashOnUse || args.Handled || !UseFlash(ent, args.User))
            return;

        args.Handled = true;
        FlashArea(ent.Owner, args.User, ent.Comp.Range, ent.Comp.AoeFlashDuration, ent.Comp.SlowTo, true, ent.Comp.Probability);
    }

    // needed for the flash lantern and interrogator lamp
    // TODO: This is awful and all the different components for toggleable lights need to be unified and changed to use Itemtoggle
    private void OnLightToggle(Entity<FlashComponent> ent, ref LightToggleEvent args)
    {
        if (!args.IsOn || !UseFlash(ent, null))
            return;

        FlashArea(ent.Owner, null, ent.Comp.Range, ent.Comp.AoeFlashDuration, ent.Comp.SlowTo, true, ent.Comp.Probability);
    }

    /// <summary>
    /// Use charges and set the visuals.
    /// </summary>
    /// <returns>False if no charges are left or the flash is currently in use.</returns>
    private bool UseFlash(Entity<FlashComponent> ent, EntityUid? user)
    {
        if (_useDelay.IsDelayed(ent.Owner))
            return false;

        if (TryComp<LimitedChargesComponent>(ent.Owner, out var charges)
            && _sharedCharges.IsEmpty((ent.Owner, charges)))
            return false;

        _sharedCharges.TryUseCharge((ent.Owner, charges));
        _audio.PlayPredicted(ent.Comp.Sound, ent.Owner, user);

        var active = EnsureComp<ActiveFlashComponent>(ent.Owner);
        active.ActiveUntil = _timing.CurTime + ent.Comp.FlashingTime;
        Dirty(ent.Owner, active);
        _appearance.SetData(ent.Owner, FlashVisuals.Flashing, true);

        if (_sharedCharges.IsEmpty((ent.Owner, charges)))
        {
            _appearance.SetData(ent.Owner, FlashVisuals.Burnt, true);
            _tag.AddTag(ent.Owner, TrashTag);
            _popup.PopupClient(Loc.GetString("flash-component-becomes-empty"), user);
        }

        return true;
    }

    /// <summary>
    /// Cause an entity to be flashed, obstructing their vision, slowing them down and stunning them.
    /// In case of a melee attack this will do a check for revolutionary conversion.
    /// </summary>
    /// <param name="target">The mob to be flashed.</param>
    /// <param name="user">The mob causing the flash, if any.</param>
    /// <param name="used">The item causing the flash, if any.</param>
    /// <param name="flashDuration">The time target will be affected by the flash.</param>
    /// <param name="slowTo">Movement speed modifier applied to the flashed target. Between 0 and 1.</param>
    /// <param name="displayPopup">Whether or not to show a popup to the target player.</param>
    /// <param name="melee">Was this flash caused by a melee attack? Used for checking for revolutionary conversion.</param>
    /// <param name="stunDuration">The time the target will be stunned. If null the target will be slowed down instead.</param>
    public void Flash(
        EntityUid target,
        EntityUid? user,
        EntityUid? used,
        TimeSpan flashDuration,
        float slowTo,
        bool displayPopup = true,
        bool melee = false,
        TimeSpan? stunDuration = null)
    {
        var attempt = new FlashAttemptEvent(target, user, used);
        RaiseLocalEvent(target, ref attempt, true);

        if (attempt.Cancelled)
            return;

        // don't paralyze, slowdown or convert to rev if the target is immune to flashes
        if (!_statusEffectsSystem.TryAddStatusEffect<FlashedComponent>(target, FlashedKey, flashDuration, true))
            return;

        if (stunDuration != null)
            _stun.TryUpdateParalyzeDuration(target, stunDuration.Value);
        else
            _movementMod.TryUpdateMovementSpeedModDuration(target, MovementModStatusSystem.FlashSlowdown, flashDuration, slowTo);

        if (displayPopup && user != null && target != user && Exists(user.Value))
        {
            _popup.PopupEntity(Loc.GetString("flash-component-user-blinds-you",
                ("user", Identity.Entity(user.Value, EntityManager))), target, target);
        }

        var ev = new AfterFlashedEvent(target, user, used, melee);
        RaiseLocalEvent(target, ref ev);

        if (user != null)
            RaiseLocalEvent(user.Value, ref ev);
        if (used != null)
            RaiseLocalEvent(used.Value, ref ev);
    }

    /// <summary>
    /// Cause all entities in range of a source entity to be flashed.
    /// </summary>
    /// <param name="source">The source of the flash, which will be at the epicenter.</param>
    /// <param name="user">The mob causing the flash, if any.</param>
    /// <param name="flashDuration">The time target will be affected by the flash.</param>
    /// <param name="slowTo">Movement speed modifier applied to the flashed target. Between 0 and 1.</param>
    /// <param name="displayPopup">Whether or not to show a popup to the target player.</param>
    /// <param name="probability">Chance to be flashed. Rolled separately for each target in range.</param>
    /// <param name="sound">Additional sound to play at the source.</param>
    public void FlashArea(EntityUid source, EntityUid? user, float range, TimeSpan flashDuration, float slowTo = 0.8f, bool displayPopup = false, float probability = 1f, SoundSpecifier? sound = null)
    {
        var transform = Transform(source);
        var mapPosition = _transform.GetMapCoordinates(transform);

        _entSet.Clear();
        _entityLookup.GetEntitiesInRange(transform.Coordinates, range, _entSet);
        foreach (var entity in _entSet)
        {
            // TODO: Use RandomPredicted https://github.com/space-wizards/RobustToolbox/pull/5849
            var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(entity).Id);
            var rand = new System.Random(seed);
            if (!rand.Prob(probability))
                continue;

            // Is the entity affected by the flash either through status effects or by taking damage?
            if (!_statusEffectsQuery.HasComponent(entity) && !_damagedByFlashingQuery.HasComponent(entity))
                continue;

            // Check for entites in view.
            // Put DamagedByFlashingComponent in the predicate because shadow anomalies block vision.
            if (!_examine.InRangeUnOccluded(entity, mapPosition, range, predicate: (e) => _damagedByFlashingQuery.HasComponent(e)))
                continue;

            Flash(entity, user, source, flashDuration, slowTo, displayPopup);
        }

        _audio.PlayPredicted(sound, source, user, AudioParams.Default.WithVolume(1f).WithMaxDistance(3f));
    }

    // Handle the flash visuals
    // TODO: Replace this with something like sprite flick once that exists to get rid of the update loop.
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<ActiveFlashComponent>();
        while (query.MoveNext(out var uid, out var active))
        {
            // reset the visuals and remove the component
            if (active.ActiveUntil < curTime)
            {
                _appearance.SetData(uid, FlashVisuals.Flashing, false);
                RemCompDeferred<ActiveFlashComponent>(uid);
            }
        }
    }

    private void OnPermanentBlindnessFlashAttempt(Entity<PermanentBlindnessComponent> ent, ref FlashAttemptEvent args)
    {
        // check for total blindness
        if (ent.Comp.Blindness == 0)
            args.Cancelled = true;
    }

    private void OnTemporaryBlindnessFlashAttempt(Entity<TemporaryBlindnessComponent> ent, ref FlashAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnFlashImmunityFlashAttempt(Entity<FlashImmunityComponent> ent, ref FlashAttemptEvent args)
    {
        if (TryComp<MaskComponent>(ent, out var mask) && mask.IsToggled)
            return;

        if (ent.Comp.Enabled)
            args.Cancelled = true;
    }

    private void OnExamine(Entity<FlashImmunityComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.ShowInExamine)
            args.PushMarkup(Loc.GetString("flash-protection"));
    }
}
