using Content.Server.DoAfter;
using Content.Server.Forensics;
using Content.Server.Polymorph.Systems;
using Content.Server.Popups;
using Content.Server.Store.Systems;
using Content.Server.Zombies;
using Content.Shared.Alert;
using Content.Shared.Changeling;
using Content.Shared.Chemistry.Components;
using Content.Shared.Cuffs.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Store.Components;
using Content.Shared.Forensics.Components;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Random;
using Content.Shared.Popups;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;
using Content.Server.Body.Systems;
using Content.Shared.Actions;
using Content.Shared.Polymorph;
using Robust.Shared.Serialization.Manager;
using Content.Server.Actions;
using Content.Server.Humanoid;
using Content.Server.Polymorph.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Server.Flash;
using Content.Server.Emp;
using Robust.Server.GameObjects;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Movement.Systems;
using Content.Shared.Damage.Systems;
using Content.Shared.Mind;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Server.Objectives.Components;
using Content.Server.Light.EntitySystems;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.StatusEffect;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Cuffs;
using Content.Shared.Fluids;
using Content.Shared.Revolutionary.Components;
using Robust.Shared.Player;
using System.Numerics;
using Content.Shared.Camera;
using Robust.Shared.Timing;
using Content.Shared.Damage.Components;
using Content.Server.Gravity;
using Content.Shared.Mobs.Components;
using Content.Server.Stunnable;
using Content.Shared.Jittering;
using System.Linq;
using Content.Shared.Radio;

namespace Content.Server.Changeling;

public sealed partial class ChangelingSystem : EntitySystem
{
    // this is one hell of a star wars intro text
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IRobustRandom _rand = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly BloodstreamSystem _blood = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly FlashSystem _flash = default!;
    [Dependency] private readonly EmpSystem _emp = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly PoweredLightSystem _light = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _recoil = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speed = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly GravitySystem _gravity = default!;
    [Dependency] private readonly BlindableSystem _blindable = default!;
    [Dependency] private readonly PullingSystem _pull = default!;
    [Dependency] private readonly SharedCuffableSystem _cuffs = default!;
    [Dependency] private readonly SharedPuddleSystem _puddle = default!;
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    [Dependency] private readonly NpcFactionSystem _factionSystem = default!;
    [Dependency] private readonly MovementModStatusSystem _movementMod = default!;

    public EntProtoId FakeArmbladePrototype = "FakeArmBladeChangeling";

    public EntProtoId BoneShardPrototype = "ThrowingStarChangeling";

    public EntProtoId ArmorPrototype = "ChangelingClothingOuterArmor";
    public EntProtoId ArmorHelmetPrototype = "ChangelingClothingHeadHelmet";

    public EntProtoId SpacesuitPrototype = "ChangelingClothingOuterHardsuit";
    public EntProtoId SpacesuitHelmetPrototype = "ChangelingClothingHeadHelmetHardsuit";

    public EntProtoId SlowdownPrototype = "StatusEffectStaminaLow";
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ChangelingComponent, MobStateChangedEvent>(OnMobStateChange);
        SubscribeLocalEvent<ChangelingComponent, DamageChangedEvent>(OnDamageChange);
        SubscribeLocalEvent<ChangelingComponent, ComponentRemove>(OnComponentRemove);

        SubscribeAbilities();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        foreach (var comp in EntityManager.EntityQuery<ChangelingComponent>())
        {
            var uid = comp.Owner;

            if (_timing.CurTime >= comp.ChemicalNextUpdateTime)
            {
                comp.ChemicalNextUpdateTime = _timing.CurTime + comp.ChemicalUpdateCooldown;
                UpdateChemicals(uid, comp);
                UpdateAbilities(uid, comp); //probably overkill since I dont think chemicals affect abilities but whatever, im cleaning up shitcode
            }

            if (_timing.CurTime >= comp.BiomassNextUpdateTime)
            {
                comp.BiomassNextUpdateTime = _timing.CurTime + comp.BiomassUpdateCooldown;
                //subtract biomass
                comp.Biomass -= comp.BiomassDrain;
                UpdateBiomass(uid, comp);
                UpdateAbilities(uid, comp);
            }
        }
    }

    private void UpdateChemicals(EntityUid uid, ChangelingComponent comp, float? amount = null)
    {
        var chemicals = comp.Chemicals;
        // either amount or regen
        chemicals += amount ?? (1 + comp.BonusChemicalRegen);
        comp.Chemicals = Math.Clamp(chemicals, 0, comp.MaxChemicals);
        Dirty(uid, comp);
        _alerts.ShowAlert(uid, "ChangelingChemicals");
    }
    private void UpdateBiomass(EntityUid uid, ChangelingComponent comp, float? amount = null)
    {
        comp.Biomass += amount ?? 0;
        comp.Biomass = Math.Clamp(comp.Biomass, 0, comp.MaxBiomass);
        Dirty(uid, comp);
        _alerts.ShowAlert(uid, "ChangelingBiomass");

        var random = _rand.Prob(0.5f);

        if (comp.Biomass <= 0)
        {
            // game over, man
            _damage.TryChangeDamage(uid, new DamageSpecifier(_proto.Index(AbsorbedDamageGroup), 50), true);
        }
        else if (comp.Biomass <= comp.MaxBiomass * comp.BiomassDeficitVomitPercent)
        {
            // THE FUNNY ITCH IS REAL!!
            comp.BonusChemicalRegen = 3f;
            _popup.PopupEntity(Loc.GetString("popup-changeling-biomass-deficit-high"), uid, uid, PopupType.LargeCaution);
            _jitter.DoJitter(uid, comp.BiomassUpdateCooldown, true, amplitude: 5, frequency: 10);

            // vomit blood
            if (random)
            {
                _movementMod.TryAddMovementSpeedModDuration(uid, SlowdownPrototype, TimeSpan.FromSeconds(1.5f), 0.5f);

                var solution = new Solution();

                var vomitAmount = 15f;
                _blood.TryModifyBloodLevel(uid, -vomitAmount);
                solution.AddReagent("Blood", vomitAmount);

                _puddle.TrySplashSpillAt(uid, Transform(uid).Coordinates, solution, out _);

                _popup.PopupEntity(Loc.GetString("disease-vomit", ("person", Identity.Entity(uid, EntityManager))), uid);
            }
        }
        else if (comp.Biomass <= comp.MaxBiomass * comp.BiomassDeficitJitterPercent)
        {
            // the funny itch is not real
            _popup.PopupEntity(Loc.GetString("popup-changeling-biomass-deficit-medium"), uid, uid, PopupType.MediumCaution);
            if (random)
            {
                _jitter.DoJitter(uid, TimeSpan.FromSeconds(.5f), true, amplitude: 5, frequency: 10);
            }
        }
        else if (comp.Biomass <= comp.MaxBiomass * comp.BiomassDeficitWarningPercent) //always do this every update
        {
            _popup.PopupEntity(Loc.GetString("popup-changeling-biomass-deficit-low"), uid, uid, PopupType.SmallCaution);
        }
        else comp.BonusChemicalRegen = 0f;
    }
    private void UpdateAbilities(EntityUid uid, ChangelingComponent comp)
    {
        _speed.RefreshMovementSpeedModifiers(uid);
        var stamina = EnsureComp<StaminaComponent>(uid);
        if (comp.StrainedMusclesActive)
        {
            _stamina.TakeStaminaDamage(uid, 7.5f, visual: false);
            if (stamina.StaminaDamage >= stamina.CritThreshold || _gravity.IsWeightless(uid))
                ToggleStrainedMuscles(uid, comp);
        }
        
        if (comp.StealthEnabled)
        {
            if (comp.Chemicals > comp.StealthDrain)
                comp.Chemicals -= comp.StealthDrain;
            else
            {
                _stamina.TakeStaminaDamage(uid, 35f, visual: false);
                if (stamina.StaminaDamage >= stamina.CritThreshold)
                    ToggleChameleonSkin(uid, comp, false);
            }
        }

        if (comp.IsInStasis)
        {
            if (comp.Biomass > comp.StasisDrain)
                comp.Biomass -= comp.StasisDrain;
            else
            {
                comp.IsInStasis = false;
                _mobState.ChangeMobState(uid, MobState.Dead);
            }
        }
    }

    #region Helper Methods

    public void PlayMeatySound(EntityUid uid, ChangelingComponent comp)
    {
        var rand = _rand.Next(0, comp.SoundPool.Count - 1);
        var sound = comp.SoundPool.ToArray()[rand];
        _audio.PlayPvs(sound, uid, AudioParams.Default.WithVolume(-3f));
    }
    public void DoScreech(EntityUid uid, ChangelingComponent comp)
    {
        _audio.PlayPvs(comp.ShriekSound, uid);

        var center = Transform(uid).MapPosition;
        var gamers = Filter.Empty();
        gamers.AddInRange(center, comp.ShriekPower, _player, EntityManager);

        foreach (var gamer in gamers.Recipients)
        {
            if (gamer.AttachedEntity == null)
                continue;

            var pos = Transform(gamer.AttachedEntity!.Value).WorldPosition;
            var delta = center.Position - pos;

            if (delta.EqualsApprox(Vector2.Zero))
                delta = new(.01f, 0);

            _recoil.KickCamera(uid, -delta.Normalized());
        }
    }

    /// <summary>
    ///     Check if a target is crit/dead or cuffed. For absorbing.
    /// </summary>
    public bool IsIncapacitated(EntityUid uid)
    {
        if (_mobState.IsIncapacitated(uid)
        || (TryComp<CuffableComponent>(uid, out var cuffs) && cuffs.CuffedHandCount > 0))
            return true;

        return false;
    }
    public bool TrySting(EntityUid uid, EntityTargetActionEvent action, bool overrideMessage = false)
    {
        var target = action.Target;

        // can't get his dna if he doesn't have it!
        if (!HasComp<AbsorbableComponent>(target) || HasComp<AbsorbedComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("changeling-sting-extract-fail"), uid, uid);
            return false;
        }

        if (HasComp<ChangelingComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("changeling-sting-fail-self", ("target", Identity.Entity(target, EntityManager))), uid, uid);
            _popup.PopupEntity(Loc.GetString("changeling-sting-fail-ling"), target, target);
            return false;
        }
        if (!overrideMessage)
            _popup.PopupEntity(Loc.GetString("changeling-sting", ("target", Identity.Entity(target, EntityManager))), uid, uid);
        return true;
    }
    public bool TryInjectReagents(EntityUid uid, Dictionary<string, FixedPoint2> reagents)
    {
        var solution = new Solution();
        foreach (var reagent in reagents)
            solution.AddReagent(reagent.Key, reagent.Value);

        if (!_solution.TryGetInjectableSolution(uid, out var targetSolution, out var _))
            return false;

        if (!_solution.TryAddSolution(targetSolution.Value, solution))
            return false;

        return true;
    }
    public bool TryReagentSting(EntityUid uid, ChangelingComponent comp, EntityTargetActionEvent action, Dictionary<string, FixedPoint2> reagents)
    {
        var target = action.Target;
        if (!TrySting(uid, action))
            return false;

        if (!TryInjectReagents(target, reagents))
            return false;

        return true;
    }
    public bool TryToggleItem(EntityUid uid, EntProtoId proto, ChangelingComponent comp, string? clothingSlot = null)
    {
        if (!comp.Equipment.TryGetValue(proto.Id, out var item))
        {
            item = Spawn(proto, Transform(uid).Coordinates);
            if (clothingSlot != null)
            {
                if (!_inventory.TryEquip(uid, (EntityUid) item, clothingSlot, force: true))
                {
                    QueueDel(item);
                    return false;
                }
                comp.Equipment.Add(proto.Id, item);
                return true;
            }
            else if (!_hands.TryForcePickupAnyHand(uid, (EntityUid) item))
            {
                _popup.PopupEntity(Loc.GetString("changeling-fail-hands"), uid, uid);
                QueueDel(item);
                return false;
            }
            comp.Equipment.Add(proto.Id, item);
            return true;
        }

        QueueDel(item);
        // assuming that it exists
        comp.Equipment.Remove(proto.Id);

        return true;
    }

    public bool TryStealDNA(EntityUid uid, EntityUid target, ChangelingComponent comp, bool countObjective = false)
    {
        if (!TryComp<HumanoidAppearanceComponent>(target, out var appearance)
        || !TryComp<MetaDataComponent>(target, out var metadata)
        || !TryComp<DnaComponent>(target, out var dna) 
        || dna.DNA == null
        || !TryComp<FingerprintComponent>(target, out var fingerprint))
            return false;

        foreach (var storedDNA in comp.AbsorbedDNA)
        {
            if (storedDNA.DNA != null && storedDNA.DNA == dna.DNA)
                return false;
        }

        var data = new TransformData
        {
            Name = metadata.EntityName,
            DNA = dna.DNA,
            Appearance = appearance
        };

        if (fingerprint.Fingerprint != null)
            data.Fingerprint = fingerprint.Fingerprint;

        if (comp.AbsorbedDNA.Count >= comp.MaxAbsorbedDNA)
            _popup.PopupEntity(Loc.GetString("changeling-sting-extract-max"), uid, uid);
        else comp.AbsorbedDNA.Add(data);

        if (countObjective
        && _mind.TryGetMind(uid, out var mindId, out var mind)
        && _mind.TryGetObjectiveComp<StealDNAConditionComponent>(mindId, out var objective, mind))
        {
            objective.DNAStolen += 1;
        }

        comp.TotalStolenDNA++;

        return true;
    }

    private ChangelingComponent? CopyChangelingComponent(EntityUid target, ChangelingComponent comp)
    {
        var newComp = EnsureComp<ChangelingComponent>(target);
        newComp.AbsorbedDNA = comp.AbsorbedDNA;
        newComp.AbsorbedDNAIndex = comp.AbsorbedDNAIndex;

        newComp.Chemicals = comp.Chemicals;
        newComp.MaxChemicals = comp.MaxChemicals;

        newComp.Biomass = comp.Biomass;
        newComp.MaxBiomass = comp.MaxBiomass;

        newComp.IsInLesserForm = comp.IsInLesserForm;
        newComp.CurrentForm = comp.CurrentForm;

        newComp.TotalAbsorbedEntities = comp.TotalAbsorbedEntities;
        newComp.TotalStolenDNA = comp.TotalStolenDNA;

        return comp;
    }
    private EntityUid? TransformEntity(EntityUid uid, TransformData? data = null, EntProtoId? protoId = null, ChangelingComponent? comp = null, bool persistentDna = false)
    {
        EntProtoId? pid = null;

        if (data != null)
        {
            if (!_proto.TryIndex(data.Appearance.Species, out var species))
                return null;
            pid = species.Prototype;
        }
        else if (protoId != null)
            pid = protoId;
        else return null;

        var config = new PolymorphConfiguration()
        {
            Entity = (EntProtoId) pid,
            TransferDamage = true,
            Forced = true,
            Inventory = PolymorphInventoryChange.Transfer,
            RevertOnCrit = false,
            RevertOnDeath = false
        };
        var newUid = _polymorph.PolymorphEntity(uid, config);

        if (newUid == null)
            return null;

        var newEnt = newUid.Value;

        if (data != null)
        {
            Comp<FingerprintComponent>(newEnt).Fingerprint = data.Fingerprint;
            Comp<DnaComponent>(newEnt).DNA = data.DNA;
            _humanoid.CloneAppearance(data.Appearance.Owner, newEnt);
            _metaData.SetEntityName(newEnt, data.Name);
            var message = Loc.GetString("changeling-transform-finish", ("target", data.Name));
            _popup.PopupEntity(message, newEnt, newEnt);
        }

        RemCompDeferred<PolymorphedEntityComponent>(newEnt);

        if (comp != null)
        {
            // copy our stuff
            var newLingComp = CopyChangelingComponent(newEnt, comp);
            if (!persistentDna && data != null)
                newLingComp?.AbsorbedDNA.Remove(data);
            RemCompDeferred<ChangelingComponent>(uid);

            if (TryComp<StoreComponent>(uid, out var storeComp))
            {
                var storeCompCopy = _serialization.CreateCopy(storeComp, notNullableOverride: true);
                RemComp<StoreComponent>(newUid.Value);
                EntityManager.AddComponent(newUid.Value, storeCompCopy);
            }
        }

        // exceptional comps check
        // there's no foreach for types i believe so i gotta thug it out yandev style.
        if (HasComp<HeadRevolutionaryComponent>(uid))
            EnsureComp<HeadRevolutionaryComponent>(newEnt);
        if (HasComp<RevolutionaryComponent>(uid))
            EnsureComp<RevolutionaryComponent>(newEnt);
        
        _factionSystem.Up(uid, newEnt);

        QueueDel(uid);

        return newUid;
    }
    public bool TryTransform(EntityUid target, ChangelingComponent comp, bool sting = false, bool persistentDna = false)
    {
        if (HasComp<AbsorbedComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("changeling-transform-fail-absorbed"), target, target);
            return false;
        }

        var data = comp.SelectedForm;

        if (data == null)
        {
            _popup.PopupEntity(Loc.GetString("changeling-transform-fail-self"), target, target);
            return false;
        }
        if (data == comp.CurrentForm)
        {
            _popup.PopupEntity(Loc.GetString("changeling-transform-fail-choose"), target, target);
            return false;
        }

        var locName = Identity.Entity(target, EntityManager);
        EntityUid? newUid = null;
        if (sting)
            newUid = TransformEntity(target, data: data, persistentDna: persistentDna);
        else newUid = TransformEntity(target, data: data, comp: comp, persistentDna: persistentDna);

        if (newUid != null)
        {
            PlayMeatySound((EntityUid) newUid, comp);
            var loc = Loc.GetString("changeling-transform-others", ("user", locName));
            _popup.PopupEntity(loc, (EntityUid) newUid, PopupType.LargeCaution);
        }

        return true;
    }

    public void RemoveAllChangelingEquipment(EntityUid target, ChangelingComponent comp)
    {
        // check if there's no entities or all entities are null
        if (comp.Equipment.Values.Count == 0
        || comp.Equipment.Values.All(ent => ent == null))
            return;

        foreach (var equip in comp.Equipment.Values)
            QueueDel(equip);

        PlayMeatySound(target, comp);
    }

    #endregion

    #region Event Handlers

    private void OnStartup(EntityUid uid, ChangelingComponent comp, ref ComponentStartup args)
    {
        RemComp<HungerComponent>(uid);
        RemComp<ThirstComponent>(uid);
        EnsureComp<ZombieImmuneComponent>(uid);

        // add actions
        foreach (var actionId in comp.BaseChangelingActions)
            _actions.AddAction(uid, actionId);

        // making sure things are right in this world
        comp.ChemicalNextUpdateTime = _timing.CurTime + comp.ChemicalUpdateCooldown;
        comp.BiomassNextUpdateTime = _timing.CurTime + comp.BiomassUpdateCooldown;

        // show alerts
        UpdateChemicals(uid, comp, 0);
        UpdateBiomass(uid, comp, 0);

        // make their blood unreal
        _blood.ChangeBloodReagent(uid, "BloodChangeling");
    }

    private void OnMobStateChange(EntityUid uid, ChangelingComponent comp, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            RemoveAllChangelingEquipment(uid, comp);
    }

    private void OnDamageChange(Entity<ChangelingComponent> ent, ref DamageChangedEvent args)
    {
        var target = args.Damageable;

        if (!TryComp<MobStateComponent>(ent, out var mobState))
            return;

        if (mobState.CurrentState != MobState.Dead)
            return;

        if (!args.DamageIncreased)
            return;

        target.Damage.ClampMax(200); // we never die. UNLESS??
    }

    private void OnComponentRemove(Entity<ChangelingComponent> ent, ref ComponentRemove args) => RemoveAllChangelingEquipment(ent, ent.Comp);
    

    #endregion
}
