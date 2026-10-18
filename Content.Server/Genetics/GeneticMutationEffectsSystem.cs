using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Electrocution;
using Content.Shared.Actions;
using Content.Shared.Atmos;
using Content.Shared.Bed.Sleep;
using Content.Shared.Body;
using Content.Shared.Chat;
using Content.Shared.Clumsy.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Electrocution;
using Content.Shared.FixedPoint;
using Content.Shared.Genetics;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Item;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Overlays;
using Content.Shared.Popups;
using Content.Shared.Paper;
using Content.Shared.Preferences;
using Content.Shared.Speech.Components;
using Content.Shared.Sprite;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Content.Shared.Eye;
using Content.Shared.Tag;
using Content.Shared.Traits.Assorted;
using Content.Shared.Weapons.Melee.Components;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Radiation.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;
using System.Numerics;

namespace Content.Server.Genetics;

public sealed partial class GeneticMutationEffectsSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private StatusEffectsSystem _statusEffect = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private IChatManager _chatManager = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedGeneticsSystem _genetics = default!;
    [Dependency] private VisibilitySystem _visibility = default!;
    [Dependency] private SharedPointLightSystem _pointLight = default!;
    [Dependency] private TransformSystem _transform = default!;
    [Dependency] private SharedContentEyeSystem _eye = default!;
    [Dependency] private SharedEyeSystem _eyeEngine = default!;
    [Dependency] private RespiratorSystem _respirator = default!;
    [Dependency] private SharedScaleVisualsSystem _scaleVisuals = default!;
    [Dependency] private AtmosphereSystem _atmos = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private HumanoidProfileSystem _humanoidProfile = default!;
    [Dependency] private SharedVisualBodySystem _visualBody = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private ElectrocutionSystem _electrocution = default!;
    [Dependency] private BarotraumaSystem _barotrauma = default!;
    [Dependency] private SharedGunSystem _gun = default!;

    private static readonly EntProtoId ClumsyStatusEffect = "StatusEffectClumsyAll";
    private static readonly EntProtoId TelekinesisActionId = "ActionGeneticTelekinesis";
    private static readonly EntProtoId TelepathyActionId = "ActionGeneticTelepathy";
    private static readonly EntProtoId ShockTouchActionId = "ActionGeneticShockTouch";
    private static readonly EntProtoId SpeciesShiftActionId = "ActionGeneticSpeciesShift";
    private static readonly EntProtoId LaserEyesActionId = "ActionGeneticLaserEyes";
    private static readonly EntProtoId LaserBoltId = "BulletLaser";
    private const string TelekinesisTag = "BypassInteractionRangeChecks";
    private const string TelepathyRadioChannel = "Binary";
    private const string StunImmunityTag = "StunImmune";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GeneticMutationComponent, DamageModifyEvent>(OnDamageModify);
        SubscribeLocalEvent<GeneticMutationComponent, DamageDealtEvent>(OnDamageDealt);
        SubscribeLocalEvent<GeneticMutationComponent, GeneticMutationAppliedEvent>(OnMutationApplied);
        SubscribeLocalEvent<GeneticMutationComponent, GeneticMutationRemovedEvent>(OnMutationRemoved);
        SubscribeLocalEvent<GeneticMutationComponent, ShotAttemptedEvent>(OnShootAttempt);
        SubscribeLocalEvent<GeneticMutationComponent, GeneticTelepathyActionEvent>(OnTelepathyAction);
        SubscribeLocalEvent<GeneticMutationComponent, GeneticTelekinesisActionEvent>(OnTelekinesisAction);
        SubscribeLocalEvent<GeneticMutationComponent, GeneticShockTouchActionEvent>(OnShockTouchAction);
        SubscribeLocalEvent<GeneticMutationComponent, GeneticSpeciesShiftActionEvent>(OnSpeciesShiftAction);
        SubscribeLocalEvent<GeneticMutationComponent, GeneticLaserEyesActionEvent>(OnLaserEyesAction);
        SubscribeLocalEvent<GeneticMutationComponent, RefreshPressureImmunityEvent>(OnRefreshPressureImmunity);
        SubscribeLocalEvent<GetMeleeDamageEvent>(OnGetMeleeDamage);
    }

    private void OnMutationApplied(Entity<GeneticMutationComponent> entity, ref GeneticMutationAppliedEvent args)
    {
        ApplyMutationEffects(entity.Owner, args.MutationId, entity.Comp);
    }

    private void OnMutationRemoved(Entity<GeneticMutationComponent> entity, ref GeneticMutationRemovedEvent args)
    {
        RemoveMutationEffects(entity.Owner, args.MutationId, entity.Comp);
    }

    private void OnDamageDealt(Entity<GeneticMutationComponent> entity, ref DamageDealtEvent args)
    {
        if (!entity.Comp.MutadoneRecoveryActive ||
            args.Damage.GetTotal() <= FixedPoint2.Zero ||
            !TryComp<ActorComponent>(entity.Owner, out var actor))
            return;

        _chatManager.DispatchServerMessage(
            actor.PlayerSession,
            Loc.GetString("genetics-mutadone-damage-message"));
    }

    private void OnShootAttempt(Entity<GeneticMutationComponent> entity, ref ShotAttemptedEvent args)
    {
        if (!entity.Comp.ActiveMutations.Any(id =>
                _proto.TryIndex<GeneticMutationPrototype>(id, out var prototype) && prototype.CanBreakWalls))
            return;

        _popup.PopupEntity(Loc.GetString("genetics-hulk-cannot-use-guns"), entity.Owner, entity.Owner);
        args.Cancel();
    }

    private void OnTelepathyAction(Entity<GeneticMutationComponent> entity, ref GeneticTelepathyActionEvent args)
    {
        if (!entity.Comp.TelepathyApplied ||
            !HasComp<GeneticTelepathyComponent>(args.Target) ||
            !_mobState.IsAlive(args.Target))
            return;

        _popup.PopupEntity(Loc.GetString("genetics-telepathy-action-target"), args.Target, args.Target);
        _popup.PopupEntity(Loc.GetString("genetics-telepathy-action-user"), entity.Owner, entity.Owner);
        args.Handled = true;
    }

    private void OnTelekinesisAction(Entity<GeneticMutationComponent> entity, ref GeneticTelekinesisActionEvent args)
    {
        if (!entity.Comp.TelekinesisApplied ||
            !HasComp<ItemComponent>(args.Target) ||
            !_hands.TryPickupAnyHand(entity.Owner, args.Target))
            return;

        args.Handled = true;
    }

    private void OnShockTouchAction(Entity<GeneticMutationComponent> entity, ref GeneticShockTouchActionEvent args)
    {
        if (!entity.Comp.ActiveMutations.Any(id =>
                _proto.TryIndex<GeneticMutationPrototype>(id, out var prototype) && prototype.GrantsShockTouch) ||
            !HasComp<MobStateComponent>(args.Target) ||
            !_mobState.IsAlive(args.Target))
            return;

        if (_electrocution.TryDoElectrocution(
                args.Target,
                entity.Owner,
                10,
                TimeSpan.FromSeconds(1),
                refresh: true))
            args.Handled = true;
    }

    private void OnLaserEyesAction(Entity<GeneticMutationComponent> entity, ref GeneticLaserEyesActionEvent args)
    {
        if (!HasActiveMutation(entity.Comp, prototype => prototype.GrantsLaserEyes))
            return;

        var origin = _transform.GetMapCoordinates(entity.Owner);
        var direction = _transform.ToMapCoordinates(args.Target).Position - origin.Position;
        if (direction.LengthSquared() < 0.01f)
            return;

        var bolt = Spawn(LaserBoltId, origin);
        _gun.ShootProjectile(bolt, direction.Normalized(), Vector2.Zero, entity.Owner, entity.Owner);
        args.Handled = true;
    }

    private void OnRefreshPressureImmunity(
        Entity<GeneticMutationComponent> entity,
        ref RefreshPressureImmunityEvent args)
    {
        if (HasActiveMutation(entity.Comp, prototype => prototype.GrantsPressureImmunity))
            args.IsImmune = true;
    }

    private bool HasActiveMutation(GeneticMutationComponent comp, Func<GeneticMutationPrototype, bool> predicate)
    {
        return comp.ActiveMutations.Any(id =>
            _proto.TryIndex<GeneticMutationPrototype>(id, out var prototype) && predicate(prototype));
    }

    private void OnSpeciesShiftAction(
        Entity<GeneticMutationComponent> entity,
        ref GeneticSpeciesShiftActionEvent args)
    {
        if (!entity.Comp.SpeciesShiftApplied || !RandomizeSpeciesShift(entity.Owner))
            return;

        args.Handled = true;
    }

    private bool RandomizeSpeciesShift(EntityUid uid)
    {
        if (!TryComp<HumanoidProfileComponent>(uid, out var humanoid))
            return false;

        ProtoId<SpeciesPrototype>[] species = { "Human", "Felinid", "SlimePerson" };
        var alternatives = species
            .Where(id => id != humanoid.Species)
            .ToArray();
        if (alternatives.Length == 0)
            return false;

        var profile = HumanoidCharacterProfile.RandomWithSpecies(_random.Pick(alternatives));
        _visualBody.ApplyProfileTo(uid, profile);
        _humanoidProfile.ApplyProfileTo(uid, profile);
        return true;
    }

    private void OnGetMeleeDamage(ref GetMeleeDamageEvent args)
    {
        if (!TryComp<GeneticMutationComponent>(args.User, out var genetics))
            return;

        foreach (var mutationId in genetics.ActiveMutations)
        {
            if (!_proto.TryIndex<GeneticMutationPrototype>(mutationId, out var prototype) ||
                prototype.MeleeBruteBonus <= 0f)
                continue;

            args.Damage.DamageDict.TryGetValue("Blunt", out var blunt);
            args.Damage.DamageDict["Blunt"] = blunt + FixedPoint2.New(
                prototype.CanBreakWalls ? Math.Max(prototype.MeleeBruteBonus, 100f) : prototype.MeleeBruteBonus);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<GeneticMutationComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            foreach (var mutationId in comp.TemporaryMutationTimers.Keys.ToArray())
            {
                var remaining = comp.TemporaryMutationTimers[mutationId] - frameTime;
                if (remaining > 0f)
                {
                    comp.TemporaryMutationTimers[mutationId] = remaining;
                    continue;
                }

                comp.TemporaryMutationTimers.Remove(mutationId);
                _genetics.TryRemoveMutation(uid, mutationId);
            }

            if (comp.ActiveMutations.Count == 0)
                continue;

            ProcessRegeneration(uid, comp, frameTime);
            ProcessEpilepsy(uid, comp, frameTime);
            ProcessTourette(uid, comp, frameTime);
            ProcessNarcolepsy(uid, comp, frameTime);
            ProcessParanoia(uid, comp, frameTime);
            ProcessNoises(uid, comp, frameTime);
            ProcessClassicMinorNegatives(uid, comp, frameTime);
            ProcessChameleon(uid, comp, frameTime);
            ProcessIronLungs(uid, comp);
            ProcessPlasmaBreathing(uid, comp);
        }
    }

    private void OnDamageModify(EntityUid uid, GeneticMutationComponent comp, DamageModifyEvent args)
    {
        foreach (var mutationId in comp.ActiveMutations)
        {
            if (!_proto.TryIndex<GeneticMutationPrototype>(mutationId, out var proto))
                continue;

            if (proto.BruteDamageMultiplier != 1f)
                ApplyGroupModifier(args.Damage.DamageDict, "Brute", proto.BruteDamageMultiplier);

            if (proto.BruteDamageModifier != 1f && proto.BruteDamageModifier < 1f)
                ApplyGroupModifier(args.Damage.DamageDict, "Brute", proto.BruteDamageModifier);

            if (proto.FireResistance > 0f)
                ApplyDictModifier(args.Damage.DamageDict, "Heat", 1f - proto.FireResistance);

            if (proto.ColdResistance > 0f)
                ApplyDictModifier(args.Damage.DamageDict, "Cold", 1f - proto.ColdResistance);

            if (proto.RadiationResistance > 0f)
                ApplyDictModifier(args.Damage.DamageDict, "Radiation", 1f - proto.RadiationResistance);
        }
    }

    private void ApplyGroupModifier(
        Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2> dict,
        string group,
        float multiplier)
    {
        foreach (var type in _proto.Index<DamageGroupPrototype>(group).DamageTypes)
        {
            if (dict.TryGetValue(type, out var existing))
                dict[type] = existing * multiplier;
        }
    }

    private static void ApplyDictModifier(
        Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2> dict,
        string key,
        float multiplier)
    {
        var damageType = new ProtoId<DamageTypePrototype>(key);
        if (dict.TryGetValue(damageType, out var existing))
            dict[damageType] = existing * multiplier;
    }

    private void AddGroupDamage(DamageSpecifier spec, string group, float amount)
    {
        var types = _proto.Index<DamageGroupPrototype>(group).DamageTypes;
        var each = FixedPoint2.New(amount / types.Count);
        foreach (var type in types)
        {
            spec.DamageDict[type] = each;
        }
    }

    private void ProcessRegeneration(EntityUid uid, GeneticMutationComponent comp, float frameTime)
    {
        comp.RegenerationTimer += frameTime;

        foreach (var mutationId in comp.ActiveMutations)
        {
            if (!_proto.TryIndex<GeneticMutationPrototype>(mutationId, out var proto))
                continue;
            if (proto.RegenerationBrute <= 0f && proto.RegenerationBurn <= 0f)
                continue;
            if (comp.RegenerationTimer < proto.RegenerationInterval)
                continue;
            if (!_mobState.IsAlive(uid) || _mobState.IsIncapacitated(uid))
                continue;

            comp.RegenerationTimer = 0f;
            var heal = new DamageSpecifier();
            if (proto.RegenerationBrute > 0f)
                AddGroupDamage(heal, "Brute", -proto.RegenerationBrute);
            if (proto.RegenerationBurn > 0f)
                AddGroupDamage(heal, "Burn", -proto.RegenerationBurn);
            _damageable.TryChangeDamage(uid, heal, interruptsDoAfters: false);
            break;
        }
    }

    private void ProcessEpilepsy(EntityUid uid, GeneticMutationComponent comp, float frameTime)
    {
        comp.EpilepsyTimer -= frameTime;
        if (comp.EpilepsyTimer > 0f)
            return;

        foreach (var mutationId in comp.ActiveMutations)
        {
            if (!_proto.TryIndex<GeneticMutationPrototype>(mutationId, out var proto))
                continue;
            if (proto.EpilepsyStunDuration <= 0f)
                continue;

            _stun.TryKnockdown(uid, TimeSpan.FromSeconds(proto.EpilepsyStunDuration), true);
            comp.EpilepsyTimer = _random.NextFloat(proto.EpilepsyMinInterval, proto.EpilepsyMaxInterval);
            break;
        }
    }

    private void ProcessTourette(EntityUid uid, GeneticMutationComponent comp, float frameTime)
    {
        comp.TouretteTimer -= frameTime;
        if (comp.TouretteTimer > 0f)
            return;

        foreach (var mutationId in comp.ActiveMutations)
        {
            if (!_proto.TryIndex<GeneticMutationPrototype>(mutationId, out var proto))
                continue;
            if (proto.TouretteExclamations.Count == 0)
                continue;

            var message = _random.Pick(proto.TouretteExclamations);
            _chat.TrySendInGameICMessage(uid, message, InGameICChatType.Speak, hideChat: false);
            comp.TouretteTimer = _random.NextFloat(proto.TouretteMinInterval, proto.TouretteMaxInterval);
            break;
        }
    }

    private void ProcessNarcolepsy(EntityUid uid, GeneticMutationComponent comp, float frameTime)
    {
        comp.NarcolepsyTimer -= frameTime;
        if (comp.NarcolepsyTimer > 0f)
            return;

        foreach (var mutationId in comp.ActiveMutations)
        {
            if (!_proto.TryIndex<GeneticMutationPrototype>(mutationId, out var proto))
                continue;
            if (proto.NarcolepsySleepDuration <= 0f)
                continue;

            _statusEffect.TryAddStatusEffectDuration(
                uid,
                SleepingSystem.StatusEffectForcedSleeping,
                TimeSpan.FromSeconds(proto.NarcolepsySleepDuration));
            comp.NarcolepsyTimer = _random.NextFloat(proto.NarcolepsyMinInterval, proto.NarcolepsyMaxInterval);
            break;
        }
    }

    private void ProcessParanoia(EntityUid uid, GeneticMutationComponent comp, float frameTime)
    {
        comp.ParanoiaTimer -= frameTime;
        if (comp.ParanoiaTimer > 0f)
            return;

        foreach (var mutationId in comp.ActiveMutations)
        {
            if (!_proto.TryIndex<GeneticMutationPrototype>(mutationId, out var proto))
                continue;
            if (proto.ParanoiaMinInterval <= 0f)
                continue;

            _popup.PopupEntity(Loc.GetString("genetics-mutation-paranoia-popup"), uid, uid, PopupType.LargeCaution);
            comp.ParanoiaTimer = _random.NextFloat(proto.ParanoiaMinInterval, proto.ParanoiaMaxInterval);
            break;
        }
    }

    private void ProcessNoises(EntityUid uid, GeneticMutationComponent comp, float frameTime)
    {
        comp.NoisesTimer -= frameTime;
        if (comp.NoisesTimer > 0f)
            return;

        foreach (var mutationId in comp.ActiveMutations)
        {
            if (!_proto.TryIndex<GeneticMutationPrototype>(mutationId, out var proto))
                continue;
            if (!proto.GrantsRandomNoises || proto.NoisesMinInterval <= 0f)
                continue;

            var noises = new[]
            {
                "genetics-mutation-noises-moo",
                "genetics-mutation-noises-bark",
                "genetics-mutation-noises-meow",
                "genetics-mutation-noises-squeak",
                "genetics-mutation-noises-oink",
            };
            _chat.TrySendInGameICMessage(uid, Loc.GetString(_random.Pick(noises)), InGameICChatType.Speak, hideChat: false);
            comp.NoisesTimer = _random.NextFloat(proto.NoisesMinInterval, proto.NoisesMaxInterval);
            break;
        }
    }

    private void ProcessClassicMinorNegatives(EntityUid uid, GeneticMutationComponent comp, float frameTime)
    {
        comp.MinorNegativeTimer -= frameTime;
        if (comp.MinorNegativeTimer > 0f)
            return;

        foreach (var mutationId in comp.ActiveMutations)
        {
            if (!_proto.TryIndex<GeneticMutationPrototype>(mutationId, out var proto) ||
                (!proto.GrantsCough && !proto.GrantsTwoLeftFeet) ||
                proto.MinorNegativeMinInterval <= 0f)
                continue;

            if (proto.GrantsCough)
                _chat.TrySendInGameICMessage(uid, Loc.GetString("genetics-mutation-cough-exclamation"),
                    InGameICChatType.Emote, hideChat: false);
            else if (proto.GrantsTwoLeftFeet)
                _stun.TryKnockdown(uid, TimeSpan.FromSeconds(proto.MinorNegativeDuration), true);

            comp.MinorNegativeTimer = _random.NextFloat(proto.MinorNegativeMinInterval, proto.MinorNegativeMaxInterval);
            break;
        }
    }

    private void ProcessChameleon(EntityUid uid, GeneticMutationComponent comp, float frameTime)
    {
        var hasChameleon = false;
        foreach (var mutationId in comp.ActiveMutations)
        {
            if (_proto.TryIndex<GeneticMutationPrototype>(mutationId, out var proto) && proto.GrantsChameleon)
            {
                hasChameleon = true;
                break;
            }
        }

        if (!hasChameleon)
        {
            if (comp.ChameleonInvisible)
                SetChameleonVisible(uid, comp, true);
            comp.ChameleonStillTimer = 0f;
            comp.HasChameleonPosition = false;
            return;
        }

        var position = _transform.GetWorldPosition(uid);
        var moving = comp.HasChameleonPosition &&
                     Vector2.DistanceSquared(position, comp.ChameleonLastPosition) > 0.01f;
        comp.ChameleonLastPosition = position;
        comp.HasChameleonPosition = true;

        if (moving)
        {
            comp.ChameleonStillTimer = 0f;
            if (comp.ChameleonInvisible)
                SetChameleonVisible(uid, comp, true);
        }
        else
        {
            comp.ChameleonStillTimer += frameTime;
            if (comp.ChameleonStillTimer >= 2f && !comp.ChameleonInvisible)
                SetChameleonVisible(uid, comp, false);
        }
    }

    private void SetChameleonVisible(EntityUid uid, GeneticMutationComponent comp, bool visible)
    {
        var vis = EnsureComp<VisibilityComponent>(uid);

        if (visible)
        {
            _visibility.RemoveLayer((uid, vis), (int)VisibilityFlags.Ghost, false);
            _visibility.AddLayer((uid, vis), (int)VisibilityFlags.Normal, false);
        }
        else
        {
            _visibility.AddLayer((uid, vis), (int)VisibilityFlags.Ghost, false);
            _visibility.RemoveLayer((uid, vis), (int)VisibilityFlags.Normal, false);
        }
        comp.ChameleonInvisible = !visible;
    }

    private void ProcessIronLungs(EntityUid uid, GeneticMutationComponent comp)
    {
        var hasIronLungs = false;
        foreach (var mutationId in comp.ActiveMutations)
        {
            if (_proto.TryIndex<GeneticMutationPrototype>(mutationId, out var proto) && proto.GrantsNoBreathing)
            {
                hasIronLungs = true;
                break;
            }
        }

        if (!hasIronLungs)
            return;

        if (!TryComp<RespiratorComponent>(uid, out var respirator))
            return;

        _respirator.UpdateSaturation(uid, respirator.MaxSaturation - respirator.Saturation, respirator);
    }

    private void ProcessPlasmaBreathing(EntityUid uid, GeneticMutationComponent comp)
    {
        var hasPlasmaBreathing = false;
        foreach (var mutationId in comp.ActiveMutations)
        {
            if (_proto.TryIndex<GeneticMutationPrototype>(mutationId, out var proto) && proto.GrantsPlasmaBreathing)
            {
                hasPlasmaBreathing = true;
                break;
            }
        }

        if (!hasPlasmaBreathing || !TryComp<RespiratorComponent>(uid, out var respirator))
            return;

        var gasMix = _atmos.GetContainingMixture(uid, excite: false);
        if (gasMix == null)
            return;

        var plasmaMoles = gasMix[(int) Gas.Plasma];
        if (plasmaMoles >= 0.5f)
            _respirator.UpdateSaturation(uid, respirator.MaxSaturation - respirator.Saturation, respirator);
    }

    public void ApplyMutationEffects(EntityUid uid, string mutationId, GeneticMutationComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;
        if (!_proto.TryIndex<GeneticMutationPrototype>(mutationId, out var proto))
            return;

        if (proto.CanBreakWalls && !comp.HulkEffectsApplied)
        {
            if (_visualBody.TryGatherMarkingsData(uid, null, out var profiles, out _, out _))
            {
                comp.HulkOriginalOrganProfiles = profiles;
                var greenProfiles = profiles.ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value with { SkinColor = Color.Green });
                _visualBody.ApplyProfiles(uid, greenProfiles);
            }

            comp.HulkAddedStunImmunity = _tag.AddTag(uid, StunImmunityTag);
            comp.HulkEffectsApplied = true;
        }

        if (proto.LightRadius > 0f && comp.BioluminescenceLight == null)
        {
            var light = Spawn(null, Transform(uid).Coordinates);
            var pointLight = EnsureComp<PointLightComponent>(light);
            _pointLight.SetRadius(light, proto.LightRadius, pointLight);
            _pointLight.SetEnergy(light, proto.LightEnergy, pointLight);
            _pointLight.SetEnabled(light, !comp.AntiGlowActive, pointLight);
            _transform.SetParent(light, uid);
            comp.BioluminescenceLight = light;
        }

        if (proto.GrantsAntiGlow)
        {
            comp.AntiGlowActive = true;
            if (comp.BioluminescenceLight is { } glow && TryComp<PointLightComponent>(glow, out var light))
                _pointLight.SetEnabled(glow, false, light);
        }

        if (proto.GrantsXRay && !comp.XRayActive)
        {
            if (TryComp<EyeComponent>(uid, out _))
                _eyeEngine.SetDrawFov(uid, false);
            comp.XRayActive = true;
        }

        if (proto.GrantsAdaptiveVision && !comp.AdaptiveVisionActive)
        {
            comp.AdaptiveVisionAdded = !HasComp<NightVisionComponent>(uid);
            if (comp.AdaptiveVisionAdded)
                AddComp<NightVisionComponent>(uid);
            comp.AdaptiveVisionActive = true;
        }

        if (proto.GrantsBlindness && !HasComp<PermanentBlindnessComponent>(uid))
        {
            AddComp<PermanentBlindnessComponent>(uid);
            comp.BlindnessAdded = true;
        }

        if (proto.GrantsMute)
        {
            var muteEffect = new EntProtoId("StatusEffectMimeMuted");
            comp.MuteApplied = !_statusEffect.HasStatusEffect(uid, muteEffect);
            if (comp.MuteApplied)
                _statusEffect.TrySetStatusEffectDuration(uid, muteEffect);
        }

        if (proto.GrantsDeafness && !HasComp<BlockListeningComponent>(uid))
        {
            AddComp<BlockListeningComponent>(uid);
            comp.DeafnessAdded = true;
        }

        if (proto.GrantsInsulation && !HasComp<InsulatedComponent>(uid))
        {
            AddComp<InsulatedComponent>(uid);
            comp.InsulationAdded = true;
        }

        if (proto.GrantsIlliteracy && !HasComp<BlockWritingComponent>(uid))
        {
            AddComp<BlockWritingComponent>(uid);
            comp.IlliteracyAdded = true;
        }

        if (proto.GrantsStuttering && !HasComp<StutteringAccentComponent>(uid))
        {
            AddComp<StutteringAccentComponent>(uid);
            comp.StutteringAdded = true;
        }

        if (proto.GrantsMobsterAccent && !HasComp<MobsterAccentComponent>(uid))
        {
            AddComp<MobsterAccentComponent>(uid);
            comp.MobsterAccentAdded = true;
        }

        if (proto.GrantsAllCapsAccent && !HasComp<AllCapsAccentComponent>(uid))
        {
            AddComp<AllCapsAccentComponent>(uid);
            comp.AllCapsAccentAdded = true;
        }

        if (proto.GrantsScrambledAccent && !HasComp<ScrambledAccentComponent>(uid))
        {
            AddComp<ScrambledAccentComponent>(uid);
            comp.ScrambledAccentAdded = true;
        }

        if (proto.GrantsOwOAccent && !HasComp<OwOAccentComponent>(uid))
        {
            AddComp<OwOAccentComponent>(uid);
            comp.OwOAccentAdded = true;
        }

        if (proto.GrantsMonkeyAccent && !HasComp<MonkeyAccentComponent>(uid))
        {
            AddComp<MonkeyAccentComponent>(uid);
            comp.MonkeyAccentAdded = true;
        }

        if (proto.GrantsMothAccent && !HasComp<MothAccentComponent>(uid))
        {
            AddComp<MothAccentComponent>(uid);
            comp.MothAccentAdded = true;
        }

        if (proto.GrantsBackwardsAccent && !HasComp<BackwardsAccentComponent>(uid))
        {
            AddComp<BackwardsAccentComponent>(uid);
            comp.BackwardsAccentAdded = true;
        }

        if (proto.GrantsNearsighted && !comp.NearsightedAdded)
        {
            _eye.SetZoom(uid, new Vector2(1.25f, 1.25f), ignoreLimits: true);
            comp.NearsightedAdded = true;
        }

        if (proto.ScaleModifier > 0f && Math.Abs(comp.AppliedScale - 1f) < 0.01f)
        {
            comp.AppliedScale = proto.ScaleModifier;
            _scaleVisuals.SetSpriteScale(uid, new Vector2(proto.ScaleModifier, proto.ScaleModifier));
        }

        if (proto.TelekinesisRange > 0f && !comp.TelekinesisApplied)
        {
            _tag.AddTag(uid, TelekinesisTag);
            _actions.AddAction(uid, ref comp.TelekinesisAction, TelekinesisActionId);
            comp.TelekinesisApplied = true;
        }

        if (proto.GrantsTelepathy && !comp.TelepathyApplied)
        {
            EnsureComp<GeneticTelepathyComponent>(uid);
            _actions.AddAction(uid, ref comp.TelepathyAction, TelepathyActionId);
            comp.TelepathyApplied = true;
        }

        if (proto.GrantsShockTouch)
            _actions.AddAction(uid, ref comp.ShockTouchAction, ShockTouchActionId);

        if (proto.GrantsLaserEyes)
            _actions.AddAction(uid, ref comp.LaserEyesAction, LaserEyesActionId);

        if (proto.GrantsRadiation && !HasComp<RadiationSourceComponent>(uid))
        {
            AddComp<RadiationSourceComponent>(uid);
            comp.RadiationAdded = true;
        }

        if (proto.GrantsPressureImmunity)
            _barotrauma.RefreshPressureImmunity(uid);

        if (proto.GrantsAnimalHearing && !comp.AnimalHearingApplied)
        {
            _eye.SetZoom(uid, new Vector2(0.8f, 0.8f), ignoreLimits: true);
            comp.AnimalHearingApplied = true;
        }

        if (proto.GrantsSpeciesShift && !comp.SpeciesShiftApplied)
        {
            if (TryComp<HumanoidProfileComponent>(uid, out var humanoid))
            {
                comp.SpeciesShiftOriginalProfile = HumanoidCharacterProfile
                    .DefaultWithSpecies(humanoid.Species, humanoid.Sex)
                    .WithName(MetaData(uid).EntityName)
                    .WithAge(humanoid.Age)
                    .WithVoice(humanoid.Voice)
                    .WithGender(humanoid.Gender);
                _visualBody.TryGatherMarkingsData(
                    uid,
                    null,
                    out var originalProfiles,
                    out _,
                    out var originalMarkings);
                comp.SpeciesShiftOriginalOrganProfiles = originalProfiles;
                comp.SpeciesShiftOriginalMarkings = originalMarkings;
                if (RandomizeSpeciesShift(uid))
                {
                    comp.SpeciesShiftApplied = true;
                    _actions.AddAction(uid, ref comp.SpeciesShiftAction, SpeciesShiftActionId);
                }
            }
        }

        if (proto.GrantsClumsiness && !comp.ClumsinessApplied)
        {
            EnsureComp<ClumsyCatchStatusEffectComponent>(uid);
            EnsureComp<ClumsyGunStatusEffectComponent>(uid);
            EnsureComp<ClumsyInjectorStatusEffectComponent>(uid);
            EnsureComp<ClumsyVaultStatusEffectComponent>(uid);
            comp.ClumsinessApplied = true;
        }

        if (proto.EpilepsyStunDuration > 0f && comp.EpilepsyTimer <= 0f)
            comp.EpilepsyTimer = _random.NextFloat(proto.EpilepsyMinInterval, proto.EpilepsyMaxInterval);
        if (proto.TouretteExclamations.Count > 0 && comp.TouretteTimer <= 0f)
            comp.TouretteTimer = _random.NextFloat(proto.TouretteMinInterval, proto.TouretteMaxInterval);
        if (proto.NarcolepsySleepDuration > 0f && comp.NarcolepsyTimer <= 0f)
            comp.NarcolepsyTimer = _random.NextFloat(proto.NarcolepsyMinInterval, proto.NarcolepsyMaxInterval);
        if (proto.ParanoiaMinInterval > 0f && comp.ParanoiaTimer <= 0f)
            comp.ParanoiaTimer = _random.NextFloat(proto.ParanoiaMinInterval, proto.ParanoiaMaxInterval);
        if (proto.GrantsRandomNoises && proto.NoisesMinInterval > 0f && comp.NoisesTimer <= 0f)
            comp.NoisesTimer = _random.NextFloat(proto.NoisesMinInterval, proto.NoisesMaxInterval);
        if (proto.MinorNegativeMinInterval > 0f && comp.MinorNegativeTimer <= 0f)
            comp.MinorNegativeTimer = _random.NextFloat(proto.MinorNegativeMinInterval, proto.MinorNegativeMaxInterval);
    }

    public void RemoveMutationEffects(EntityUid uid, string mutationId, GeneticMutationComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;
        if (!_proto.TryIndex<GeneticMutationPrototype>(mutationId, out var proto))
            return;

        if (proto.CanBreakWalls &&
            !comp.ActiveMutations.Any(id =>
                _proto.TryIndex<GeneticMutationPrototype>(id, out var active) && active.CanBreakWalls))
        {
            if (comp.HulkOriginalOrganProfiles != null)
            {
                _visualBody.ApplyProfiles(uid, comp.HulkOriginalOrganProfiles);
                comp.HulkOriginalOrganProfiles = null;
            }

            if (comp.HulkAddedStunImmunity)
                _tag.RemoveTag(uid, StunImmunityTag);
            comp.HulkAddedStunImmunity = false;
            comp.HulkEffectsApplied = false;
        }

        if (proto.LightRadius > 0f && comp.BioluminescenceLight != null)
        {
            QueueDel(comp.BioluminescenceLight.Value);
            comp.BioluminescenceLight = null;
        }

        if (proto.GrantsAntiGlow)
        {
            comp.AntiGlowActive = false;
            if (comp.BioluminescenceLight is { } glow && TryComp<PointLightComponent>(glow, out var light))
                _pointLight.SetEnabled(glow, true, light);
        }

        if (proto.GrantsXRay && comp.XRayActive)
        {
            var stillHasXRay = false;
            foreach (var id in comp.ActiveMutations)
            {
                if (id == mutationId) continue;
                if (_proto.TryIndex<GeneticMutationPrototype>(id, out var p) && p.GrantsXRay)
                {
                    stillHasXRay = true;
                    break;
                }
            }
            if (!stillHasXRay)
            {
                if (TryComp<EyeComponent>(uid, out _))
                    _eyeEngine.SetDrawFov(uid, true);
                comp.XRayActive = false;
            }
        }

        if (proto.GrantsAdaptiveVision && comp.AdaptiveVisionActive)
        {
            comp.AdaptiveVisionActive = false;
            if (comp.AdaptiveVisionAdded)
                RemComp<NightVisionComponent>(uid);
            comp.AdaptiveVisionAdded = false;
        }

        if (proto.ScaleModifier > 0f && Math.Abs(comp.AppliedScale - proto.ScaleModifier) < 0.01f)
        {
            comp.AppliedScale = 1f;
            var stillHasScale = false;
            foreach (var id in comp.ActiveMutations)
            {
                if (id == mutationId) continue;
                if (_proto.TryIndex<GeneticMutationPrototype>(id, out var p) && p.ScaleModifier > 0f)
                {
                    stillHasScale = true;
                    break;
                }
            }
            if (!stillHasScale)
                _scaleVisuals.SetSpriteScale(uid, Vector2.One);
        }

        if (proto.MeleeBruteBonus > 0f && TryComp<BonusMeleeDamageComponent>(uid, out _))
        {
            var stillHasHulk = false;
            foreach (var id in comp.ActiveMutations)
            {
                if (id == mutationId) continue;
                if (_proto.TryIndex<GeneticMutationPrototype>(id, out var p) && p.MeleeBruteBonus > 0f)
                {
                    stillHasHulk = true;
                    break;
                }
            }
            if (!stillHasHulk)
                RemComp<BonusMeleeDamageComponent>(uid);
        }

        if (proto.TelekinesisRange > 0f && comp.TelekinesisApplied)
        {
            var stillHasTk = false;
            foreach (var id in comp.ActiveMutations)
            {
                if (id == mutationId) continue;
                if (_proto.TryIndex<GeneticMutationPrototype>(id, out var p) && p.TelekinesisRange > 0f)
                {
                    stillHasTk = true;
                    break;
                }
            }
            if (!stillHasTk)
            {
                _tag.RemoveTag(uid, TelekinesisTag);
                if (comp.TelekinesisAction is { } action)
                    _actions.RemoveAction((uid, null), (action, null));
                comp.TelekinesisAction = null;
                comp.TelekinesisApplied = false;
            }
        }

        if (proto.GrantsTelepathy && comp.TelepathyApplied)
        {
            var stillHasTelepath = false;
            foreach (var id in comp.ActiveMutations)
            {
                if (id == mutationId) continue;
                if (_proto.TryIndex<GeneticMutationPrototype>(id, out var p) && p.GrantsTelepathy)
                {
                    stillHasTelepath = true;
                    break;
                }
            }
            if (!stillHasTelepath)
            {
                RemComp<GeneticTelepathyComponent>(uid);
                if (comp.TelepathyAction is { } action)
                    _actions.RemoveAction((uid, null), (action, null));
                comp.TelepathyAction = null;
                comp.TelepathyApplied = false;
            }
        }

        if (proto.GrantsShockTouch && comp.ShockTouchAction is { } shockAction)
        {
            _actions.RemoveAction((uid, null), (shockAction, null));
            comp.ShockTouchAction = null;
        }

        if (proto.GrantsLaserEyes && comp.LaserEyesAction is { } laserAction)
        {
            _actions.RemoveAction((uid, null), (laserAction, null));
            comp.LaserEyesAction = null;
        }

        if (proto.GrantsRadiation && comp.RadiationAdded)
        {
            RemComp<RadiationSourceComponent>(uid);
            comp.RadiationAdded = false;
        }

        if (proto.GrantsPressureImmunity)
            _barotrauma.RefreshPressureImmunity(uid);

        if (proto.GrantsAnimalHearing && comp.AnimalHearingApplied)
        {
            var stillHasHearing = false;
            foreach (var id in comp.ActiveMutations)
            {
                if (id == mutationId) continue;
                if (_proto.TryIndex<GeneticMutationPrototype>(id, out var p) && p.GrantsAnimalHearing)
                {
                    stillHasHearing = true;
                    break;
                }
            }
            if (!stillHasHearing)
            {
                _eye.ResetZoom(uid);
                comp.AnimalHearingApplied = false;
            }
        }

        if (proto.GrantsSpeciesShift && comp.SpeciesShiftApplied)
        {
            if (comp.SpeciesShiftOriginalProfile != null && HasComp<HumanoidProfileComponent>(uid))
            {
                _visualBody.ApplyProfileTo(uid, comp.SpeciesShiftOriginalProfile);
                _humanoidProfile.ApplyProfileTo(uid, comp.SpeciesShiftOriginalProfile);
            }
            if (comp.SpeciesShiftOriginalOrganProfiles != null)
                _visualBody.ApplyProfiles(uid, comp.SpeciesShiftOriginalOrganProfiles);
            if (comp.SpeciesShiftOriginalMarkings != null)
                _visualBody.ApplyMarkings(uid, comp.SpeciesShiftOriginalMarkings);
            comp.SpeciesShiftOriginalProfile = null;
            comp.SpeciesShiftOriginalOrganProfiles = null;
            comp.SpeciesShiftOriginalMarkings = null;
            comp.SpeciesShiftApplied = false;
            if (comp.SpeciesShiftAction is { } action)
                _actions.RemoveAction((uid, null), (action, null));
            comp.SpeciesShiftAction = null;
        }

        if (proto.GrantsClumsiness && comp.ClumsinessApplied)
        {
            var stillHasClumsy = false;
            foreach (var id in comp.ActiveMutations)
            {
                if (id == mutationId) continue;
                if (_proto.TryIndex<GeneticMutationPrototype>(id, out var p) && p.GrantsClumsiness)
                {
                    stillHasClumsy = true;
                    break;
                }
            }
            if (!stillHasClumsy)
            {
                RemComp<ClumsyCatchStatusEffectComponent>(uid);
                RemComp<ClumsyGunStatusEffectComponent>(uid);
                RemComp<ClumsyInjectorStatusEffectComponent>(uid);
                RemComp<ClumsyVaultStatusEffectComponent>(uid);
                comp.ClumsinessApplied = false;
            }
        }

        if (proto.GrantsChameleon && comp.ChameleonInvisible)
        {
            SetChameleonVisible(uid, comp, true);
            comp.ChameleonStillTimer = 0f;
            comp.HasChameleonPosition = false;
        }

        if (proto.GrantsBlindness && comp.BlindnessAdded)
        {
            RemComp<PermanentBlindnessComponent>(uid);
            comp.BlindnessAdded = false;
        }

        if (proto.GrantsMute && comp.MuteApplied)
        {
            _statusEffect.TryRemoveStatusEffect(uid, new EntProtoId("StatusEffectMimeMuted"));
            comp.MuteApplied = false;
        }

        if (proto.GrantsDeafness && comp.DeafnessAdded)
        {
            RemComp<BlockListeningComponent>(uid);
            comp.DeafnessAdded = false;
        }

        if (proto.GrantsInsulation && comp.InsulationAdded)
        {
            RemComp<InsulatedComponent>(uid);
            comp.InsulationAdded = false;
        }

        if (proto.GrantsIlliteracy && comp.IlliteracyAdded)
        {
            RemComp<BlockWritingComponent>(uid);
            comp.IlliteracyAdded = false;
        }

        if (proto.GrantsStuttering && comp.StutteringAdded)
        {
            RemComp<StutteringAccentComponent>(uid);
            comp.StutteringAdded = false;
        }

        if (proto.GrantsMobsterAccent && comp.MobsterAccentAdded)
        {
            RemComp<MobsterAccentComponent>(uid);
            comp.MobsterAccentAdded = false;
        }

        if (proto.GrantsAllCapsAccent && comp.AllCapsAccentAdded)
        {
            RemComp<AllCapsAccentComponent>(uid);
            comp.AllCapsAccentAdded = false;
        }

        if (proto.GrantsScrambledAccent && comp.ScrambledAccentAdded)
        {
            RemComp<ScrambledAccentComponent>(uid);
            comp.ScrambledAccentAdded = false;
        }

        if (proto.GrantsOwOAccent && comp.OwOAccentAdded)
        {
            RemComp<OwOAccentComponent>(uid);
            comp.OwOAccentAdded = false;
        }

        if (proto.GrantsMonkeyAccent && comp.MonkeyAccentAdded)
        {
            RemComp<MonkeyAccentComponent>(uid);
            comp.MonkeyAccentAdded = false;
        }

        if (proto.GrantsMothAccent && comp.MothAccentAdded)
        {
            RemComp<MothAccentComponent>(uid);
            comp.MothAccentAdded = false;
        }

        if (proto.GrantsBackwardsAccent && comp.BackwardsAccentAdded)
        {
            RemComp<BackwardsAccentComponent>(uid);
            comp.BackwardsAccentAdded = false;
        }

        if (proto.GrantsNearsighted && comp.NearsightedAdded)
        {
            _eye.ResetZoom(uid);
            comp.NearsightedAdded = false;
        }
    }
}
