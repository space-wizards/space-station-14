using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.FixedPoint;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Projectiles;
using Content.Shared.Chat;
using Content.Shared.Mobs.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Rejuvenate;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing; 
using Content.Shared.Examine;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Fluids.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Storage;
using Robust.Shared.Containers;
using Robust.Shared.Audio.Systems;
using Content.Shared.Popups;
using Content.Shared.StatusIcon;
using Content.Shared.Mind;
using Content.Shared.Roles.Jobs;
using Content.Shared.Overlays;

namespace Content.Shared.EmotionalState;

public sealed class EmotionalStateSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly MobThresholdSystem _mobThresholdSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedHandsSystem _handSystem = default!;
    [Dependency] private readonly SharedSuicideSystem _suicideSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _sharedAppearanceSystem = default!;
    [Dependency] private readonly SharedPopupSystem _sharedPopupSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly SharedJobSystem _jobSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    private HashSet<EntityUid> _entitiesInRange = new();
    private HashSet<EntityUid> _weaponInRange = new();
    private Dictionary<String, (bool, (List<String>, List<String>))> _dataTableTriggers = new Dictionary<String, (bool, (List<String>, List<String>))>();
    private Dictionary<String, int> _speciesCounter = new Dictionary<String, int>();
    private Dictionary<String, List<String>> _reagentTriggers = new Dictionary<String, List<String>>();
    private float _plusEmot = 0.0f;
    private float _minEmot = 0.0f;
    private float _plusEmotPsyCard = 0.0f;
    private float _minEmotPsyCard = 0.0f;
    private bool _dtEmpty = true;

    // icons to indicate a current increase in emotional state
    private static readonly ProtoId<EmotionalStateIconPrototype> EmotionalStateIconZerolId = "EmotionalStateIconZero";
    private static readonly ProtoId<EmotionalStateIconPrototype> EmotionalStateIconMinus1Id = "EmotionalStateIconMinus1";
    private static readonly ProtoId<EmotionalStateIconPrototype> EmotionalStateIconMinus2Id = "EmotionalStateIconMinus2";
    private static readonly ProtoId<EmotionalStateIconPrototype> EmotionalStateIconMinus3Id = "EmotionalStateIconMinus3";
    private static readonly ProtoId<EmotionalStateIconPrototype> EmotionalStateIconPlus1Id = "EmotionalStateIconPlus1";
    private static readonly ProtoId<EmotionalStateIconPrototype> EmotionalStateIconPlus2Id = "EmotionalStateIconPlus2";
    private static readonly ProtoId<EmotionalStateIconPrototype> EmotionalStateIconPlus3Id = "EmotionalStateIconPlus3";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmotionalStateComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<EmotionalStateComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<EmotionalStateComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<EmotionalStateComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
        SubscribeLocalEvent<EmotionalStateComponent, RejuvenateEvent>(OnRejuvenate);

        SubscribeLocalEvent<PsychologyCardComponent, MapInitEvent>(OnMapPsyCardInit);
        SubscribeLocalEvent<PsychologyCardComponent, ComponentInit>(OnComponentPsyCardInit);
        SubscribeLocalEvent<PsychologyCardComponent, ExaminedEvent>(OnExamine);
    }

    private void OnMapInit(EntityUid uid, EmotionalStateComponent component, MapInitEvent args)
    {
        var amount = _random.Next(
            (int)component.Thresholds[EmotionalThreshold.Neutral],
            (int)component.Thresholds[EmotionalThreshold.Excellent] + 1);
        component.NextThresholdUpdateTime = _timing.CurTime + component.ThresholdUpdateRate;

        if (_dtEmpty) { CreateDT(); }
        if (component.Triggers.Count == 0)
        {
            if (TrySelectTriggers(uid, component))
                SetEmotionalState(uid, amount, component);
        }
    }

    private void OnComponentInit(EntityUid uid, EmotionalStateComponent component, ref ComponentInit args)
    {
        var amount = _random.Next(
            (int)component.Thresholds[EmotionalThreshold.Neutral],
            (int)component.Thresholds[EmotionalThreshold.Excellent] + 1);
        component.NextThresholdUpdateTime = _timing.CurTime + component.ThresholdUpdateRate;

        if (_dtEmpty) { CreateDT(); }
        if (component.Triggers.Count == 0)
        {
            if (TrySelectTriggers(uid, component))
                SetEmotionalState(uid, amount, component);
        }
    }

    private void OnMapPsyCardInit(EntityUid uid, PsychologyCardComponent component, MapInitEvent args)
    {
        var meta = MetaData(uid);

        var description = "";
        var name = "image";
        foreach (var descItem in component.Triggers)
        {
            if (_prototype.TryIndex<EntityPrototype>(descItem, out var protoDesc) && protoDesc.Name != null)
            {
                description += protoDesc.Name + "; ";
                name = protoDesc.Name;
            }
            else
            {
                name = descItem;
                description += descItem + "; ";
            }
        }

        if (String.IsNullOrEmpty(component.LocName))
        {
            _metaData.SetEntityName(uid, Loc.GetString("psychology-card-name", ("name", name)), meta);
        }
        else
        {
            _metaData.SetEntityName(uid, Loc.GetString(component.LocName), meta);
        }

        _metaData.SetEntityDescription(uid, Loc.GetString("psychology-card-description-without-suffix", ("description", description)), meta);
    }

    private void OnComponentPsyCardInit(EntityUid uid, PsychologyCardComponent component, ComponentInit args)
    {
        var meta = MetaData(uid);

        var description = "";
        var name = "image";
        foreach(var descItem in component.Triggers)
        {
            if (_prototype.TryIndex<EntityPrototype>(descItem, out var protoDesc) && protoDesc.Name != null)
            {
                description += protoDesc.Name + "; ";
                name = protoDesc.Name;
            }
            else
            {
                name = descItem;
                description += descItem + "; ";
            }
        }

        if (String.IsNullOrEmpty(component.LocName))
        {
            _metaData.SetEntityName(uid, Loc.GetString("psychology-card-name", ("name", name)), meta);
        }
        else
        {
            _metaData.SetEntityName(uid, Loc.GetString(component.LocName), meta);
        }

        _metaData.SetEntityDescription(uid, Loc.GetString("psychology-card-description-without-suffix", ("description", description)), meta);
    }

    private void OnExamine(EntityUid uid, PsychologyCardComponent component, ExaminedEvent args)
    {
        var meta = MetaData(uid);
        if (meta.EntityPrototype != null && meta.EntityPrototype.EditorSuffix != null)
        {
            args.PushMarkup(Loc.GetString("psychology-card-description-with-suffix", ("suffix", meta.EntityPrototype.EditorSuffix)));
        }
    }

    /// <summary>
    /// Attempts to select triggers that are applicable to the current player (role, race) from the common pool.
    /// Returns true if successful; otherwise, removes the <see cref="EmotionalStateComponent"/>.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    private bool TrySelectTriggers(EntityUid uid, EmotionalStateComponent component)
    {
        if (_dtEmpty)
            return false;

        if (!_entityManager.TryGetComponent<MetaDataComponent>(uid, out var metaData))
        {
            Log.Warning($"Entity {uid} haven't MetaDataComponent");
            RemComp<EmotionalStateComponent>(uid);
            return false;
        }

        if (metaData.EntityPrototype == null)
        {
            RemComp<EmotionalStateComponent>(uid);
            return false;
        }

        String speciesId = "MobHuman";

        if (metaData.EntityPrototype.ID != null)
        {
            speciesId = metaData.EntityPrototype.ID;
        }
        else
        {
            RemComp<EmotionalStateComponent>(uid);
            return false;
        }

        if (!_speciesCounter.ContainsKey(speciesId))
        {
            Log.Warning($"Entity {uid} haven't trigger's prototype"); /// If no prototype was found for it, then the entity doesn't need an emotional state in the first place.
            RemComp<EmotionalStateComponent>(uid);                    /// Although, on the other hand, the entity could still receive emotional damage from having
            return false;                                             /// multiple body injuries or from events.
        }

        var filteredItems = _dataTableTriggers
            .Where(x => x.Value.Item2.Item1.Contains(speciesId))
            .ToList();

        var requiredFilteredItems = _dataTableTriggers
            .Where(x => x.Value.Item1 && x.Value.Item2.Item1.Contains(speciesId))
            .ToList();

        if (_mindSystem.TryGetMind(uid, out var mindId, out var mind) && mindId != null)
        {  
            if (_jobSystem.MindTryGetJobId(mindId, out var jobId) && jobId != null)
            {
                string jobIdString = jobId;

                filteredItems = _dataTableTriggers
                    .Where(x => x.Value.Item2.Item1.Contains(speciesId) && !x.Value.Item2.Item2.Contains(jobIdString))
                    .ToList();

                requiredFilteredItems = _dataTableTriggers
                    .Where(x => x.Value.Item1 && x.Value.Item2.Item1.Contains(speciesId) && !x.Value.Item2.Item2.Contains(jobIdString))
                    .ToList();
            }
        }

        // We shuffle using the Fisher-Yates algorithm for random trigger selection.
        for (int i = filteredItems.Count - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            var temp = filteredItems[i];
            filteredItems[i] = filteredItems[j];
            filteredItems[j] = temp;
        }

        int numberOfTriggers = _random.Next(0, filteredItems.Count + 1);

        if (numberOfTriggers > 0)
        {
            var randomTriggers = filteredItems.Take(numberOfTriggers).ToList();

            foreach (var trigger in randomTriggers)
            {
                var triggerPrototype = _prototype.Index<EmotionalTriggersPrototype>(trigger.Key).TriggersPrototype;
                var positEffect = _prototype.Index<EmotionalTriggersPrototype>(trigger.Key).PositEffect;
                var negatEffect = _prototype.Index<EmotionalTriggersPrototype>(trigger.Key).NegatEffect;

                if (triggerPrototype == null || positEffect == null || negatEffect == null)
                    continue;

                if (component.Triggers.ContainsKey(triggerPrototype))
                    continue;

                component.Triggers.Add(triggerPrototype, new float[] { positEffect, negatEffect });
            }
        }
        else
        {
            component.Fearless = true; // Theoretically, it might turn out that the current role is excluded by all triggers. Fearless :)
        }

        if (requiredFilteredItems.Count > 0)
        {
            foreach (var reqTrigger in requiredFilteredItems)
            {
                var reqTriggerPrototype = _prototype.Index<EmotionalTriggersPrototype>(reqTrigger.Key).TriggersPrototype;
                var reqTpositEffect = _prototype.Index<EmotionalTriggersPrototype>(reqTrigger.Key).PositEffect;
                var reqTnegatEffect = _prototype.Index<EmotionalTriggersPrototype>(reqTrigger.Key).NegatEffect;

                if (reqTriggerPrototype == null || reqTpositEffect == null || reqTnegatEffect == null)
                    continue;

                if (component.Triggers.ContainsKey(reqTriggerPrototype))
                    continue;

                component.Triggers.Add(reqTriggerPrototype, new float[] { reqTpositEffect, reqTnegatEffect });
            }

            component.Fearless = false;
        }

        // This is still common for members of the same race due to similar formative conditions.
        foreach (var triggerReagent in _reagentTriggers)
        {
            if (triggerReagent.Value.Contains(speciesId))
            {
                var triggersProt = _prototype.Index<EmotionalTriggersReagentPrototype>(triggerReagent.Key).TriggersPrototype;

                if (component.TriggersReagent.ContainsKey(triggersProt))
                    continue;

                component.TriggersReagent.Add(triggersProt, triggerReagent.Key);
            }
        }

        return true;
    }


    /// <summary>
    /// Populates 2 dictionaries with trigger prototypes for further selection and 1 auxiliary dictionary.
    /// </summary>
    private void CreateDT()
    {
        foreach (var prototype in _prototype.EnumeratePrototypes<EmotionalTriggersPrototype>())
        {
            if (prototype.Abstract)
                continue;

            if (_dataTableTriggers.ContainsKey(prototype.ID))
                continue;

            if (prototype.Species.Count == 0)
                continue;

            var tempPairValue = (prototype.RequiredTrigger, (prototype.Species, prototype.JobsExept));
            _dataTableTriggers.Add(prototype.ID, tempPairValue);

            foreach (var species in prototype.Species)
            {
                if (_speciesCounter.ContainsKey(species))
                {
                    _speciesCounter[species] += 1;
                }
                else
                {
                    _speciesCounter.Add(species, 1);
                }
            }
        }

        foreach (var reagentTriggersProt in _prototype.EnumeratePrototypes<EmotionalTriggersReagentPrototype>())
        {
            if (_reagentTriggers.ContainsKey(reagentTriggersProt.ID))
                continue;

            _reagentTriggers.Add(reagentTriggersProt.ID, reagentTriggersProt.Species);
        }

        if (_dataTableTriggers.Capacity == 0 && _reagentTriggers.Capacity == 0)
        {
            var query = EntityQueryEnumerator<EmotionalStateComponent>();
            while (query.MoveNext(out var uid, out var emotionalState))
            {
                RemComp<EmotionalStateComponent>(uid);
            }
        }

        _dtEmpty = false;
    }

    private void OnShutdown(EntityUid uid, EmotionalStateComponent component, ComponentShutdown args)
    {
        _alerts.ClearAlertCategory(uid, component.EmotionalAlertCategory);
    }

    /// <summary>
    /// Applies the speed modifier defined in <see cref="EmotionalStateComponent.SpeedModifer"/>.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <param name="args"></param>
    private void OnRefreshMovespeed(EntityUid uid, EmotionalStateComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (component.CurrentThreshold == component.LastThreshold)
            return;

        args.ModifySpeed(component.SpeedModifer[component.CurrentThreshold], component.SpeedModifer[component.CurrentThreshold]);
    }

    private void OnRejuvenate(EntityUid uid, EmotionalStateComponent component, RejuvenateEvent args)
    {
        SetEmotionalState(uid, component.Thresholds[EmotionalThreshold.Good], component);
    }

    /// <summary>
    /// Adds to the current emotional state the values calculated in the UpdateCurrentThreshold() method and multiplied by the current state
    /// modifiers. <see cref="EmotionalStateComponent.EmotionalPositiveDecayModifiers"/> <see cref="EmotionalStateComponent.EmotionalNegativeDecayModifiers"/>.
    /// </summary>
    /// <param name="component"></param>
    public float GetEmotionalState(EmotionalStateComponent component)
    {
        var value = component.LastAuthoritativeEmotionalValue;
        var deltaValue = _plusEmot * component.EmotionalPositiveDecayModifiers[component.CurrentThreshold] - _minEmot * component.EmotionalNegativeDecayModifiers[component.CurrentThreshold];

        value += deltaValue;
        component.LastDeltaAuthoritativeEmotionalValue = deltaValue + _plusEmotPsyCard - _minEmotPsyCard;
        component.LastAuthoritativeEmotionalValue = ClampEmotionalStateValueWithinThresholds(component, value);

        _plusEmot = 0;
        _plusEmotPsyCard = 0;
        _minEmot = 0;
        _minEmotPsyCard = 0;

        return ClampEmotionalStateValueWithinThresholds(component, value);
    }

    /// <summary>
    /// Sets the current value of the emotional state.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="amount"></param>
    /// <param name="component"></param>
    public void SetEmotionalState(EntityUid uid, float amount, EmotionalStateComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        SetAuthoritativeEmotionalStateValue((uid, component), amount);
        UpdateCurrentThreshold(uid, component);
    }

    /// <summary>
    /// Sets <see cref="EmotionalStateComponent.LastEmotionalValue"/> and
    /// <see cref="EmotionalStateComponent.LastAuthoritativeEmotionalChangeTime"/>, and dirties this entity. This "resets" the
    /// starting point for <see cref="GetEmotionalState"/>'s calculation.
    /// </summary>
    /// <param name="entity">The entity whose emotional state will be set.</param>
    /// <param name="value">The value to set the entity's emotional state to.</param>
    private void SetAuthoritativeEmotionalStateValue(Entity<EmotionalStateComponent> entity, float value)
    {
        entity.Comp.LastAuthoritativeEmotionalChangeTime = _timing.CurTime;
        entity.Comp.LastAuthoritativeEmotionalValue = ClampEmotionalStateValueWithinThresholds(entity.Comp, value);
        DirtyField(entity.Owner, entity.Comp, nameof(EmotionalStateComponent.LastAuthoritativeEmotionalChangeTime));
        DirtyField(entity.Owner, entity.Comp, nameof(EmotionalStateComponent.LastAuthoritativeEmotionalValue));
    }

    /// <summary>
    /// The main method for calculating the addition to the current emotional state.
    /// This method checks whether the entity's prototype is in the list of triggers for this player.
    /// It considers entities within the allowable radius (field of view <see cref="EmotionalStateComponent.RangeTrigger"/>)
    /// that the player can see unobstructed. It also checks the current health level (if damage is 5% of health down to critical,
    /// the player loses emotional state points) and the contents of the organism for "triggering" reagents.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    private void UpdateCurrentThreshold(EntityUid uid, EmotionalStateComponent component)
    {
        _entitiesInRange.Clear();
        var Coordinates = _entityManager.GetComponent<TransformComponent>(uid).Coordinates;
        _entityLookup.GetEntitiesInRange(Coordinates, component.RangeTrigger, _entitiesInRange, flags: LookupFlags.Uncontained);

        foreach (var entity in _entitiesInRange)
        {
            if (component.Fearless)
                break;

            if (entity == uid)
                continue;

            // we check the slots that the player can logically "see". For example, a clown's mask.
            var slotEnumerator = _inventory.GetSlotEnumerator(entity);
            while (slotEnumerator.NextItem(out var item, out var slot))
            {
                if (slot.Name == "head" ||
                    slot.Name == "eyes" ||
                    slot.Name == "ears" ||
                    slot.Name == "mask" ||
                    slot.Name == "outerClothing" ||
                    slot.Name == "jumpsuit" ||
                    slot.Name == "neck" ||
                    slot.Name == "back" ||
                    slot.Name == "belt" ||
                    slot.Name == "gloves" ||
                    slot.Name == "shoes")
                {
                    if (_entityManager.TryGetComponent<MetaDataComponent>(item, out var metaData))
                    {
                        if (metaData.EntityPrototype is null)
                            continue;

                        if (component.Triggers.ContainsKey(metaData.EntityPrototype.ID))
                        {
                            _plusEmot += component.Triggers[metaData.EntityPrototype.ID][0];
                            _minEmot += component.Triggers[metaData.EntityPrototype.ID][1];
                        }
                    }
                }
            }

            // InRangeUnOccluded() returns true if the object can be seen (we are looking for objects that the player "sees").
            if (_examine.InRangeUnOccluded(uid, entity, component.RangeTrigger))
            {
                // we check puddles for triggering potential
                if (_entityManager.TryGetComponent<PuddleComponent>(entity, out var puddleComponent) && puddleComponent != null)
                {
                    _minEmot += 0.1f; // the puddle itself should be unpleasant

                    // we check the contents of the puddles, as someone might be frightened by slime, while someone else by blood (each race dislikes what spills out of their kin)
                    if (puddleComponent.Solution != null)
                    {
                        if (puddleComponent != null)
                        {
                            Entity<SolutionComponent> solutionEntity = (Entity<SolutionComponent>)puddleComponent.Solution;
                            Solution solution = solutionEntity.Comp.Solution;

                            foreach (var (reagent, quantity) in solution)
                            {
                                var reagentProto = _prototype.Index<ReagentPrototype>(reagent.Prototype);

                                if (component.Triggers.ContainsKey(reagentProto.ID))
                                {
                                    _plusEmot += component.Triggers[reagentProto.ID][0];
                                    _minEmot += component.Triggers[reagentProto.ID][1];
                                }
                            }

                            continue;
                        }
                    }
                }

                // we check other visible entities for triggering potential
                if (_entityManager.TryGetComponent<MetaDataComponent>(entity, out var metaDataComponent))
                {
                    if (metaDataComponent.EntityPrototype is null)
                        continue;

                    /// Psychologist cards contain the IDs of the prototypes "depicted" on them.
                    /// This trigger identification tool should provide a strong increase or a strong decrease in the emotional state.
                    /// The psychologist will determine this using the visor and icons to understand the therapeutic environment.
                    if (_entityManager.TryGetComponent<PsychologyCardComponent>(entity, out var psychologyCardComponent))
                    {
                        if (psychologyCardComponent.Triggers.Count == 0 || psychologyCardComponent.Triggers is null)
                            continue;

                        foreach (var trigerCard in psychologyCardComponent.Triggers)
                        {
                            if (component.Triggers.ContainsKey(trigerCard))
                            {
                                if (component.Triggers[trigerCard][0] > 0)
                                {
                                    _plusEmotPsyCard += psychologyCardComponent.EmotionalEffect;
                                }

                                if (component.Triggers[trigerCard][1] > 0)
                                {
                                    _minEmotPsyCard += psychologyCardComponent.EmotionalEffect;
                                }

                                break;
                            }
                        }
                    }

                    if (component.Triggers.ContainsKey(metaDataComponent.EntityPrototype.ID))
                    {
                        _plusEmot += component.Triggers[metaDataComponent.EntityPrototype.ID][0];
                        _minEmot += component.Triggers[metaDataComponent.EntityPrototype.ID][1];
                    }
                }
            }

        }

        // checking health for triggering potential
        if (_entityManager.TryGetComponent<DamageableComponent>(uid, out var damageableComponent))
        {
            if (_mobThresholdSystem.TryGetDeadPercentage(uid, FixedPoint2.Max(0.0, damageableComponent.TotalDamage), out var critLevel))
            {
                if (critLevel > 0.05) // good health -> okay health
                {
                    _minEmot += 0.25f;
                }
            }
        }

        // checking the organism's contents for triggering potential
        if (_entityManager.TryGetComponent(uid, out SolutionContainerManagerComponent? container)
            && container.Containers.Count > 0
            && _entityManager.TryGetComponent(uid, out HungerComponent? hungComp))
        {
            foreach (var (name, solution) in _solution.EnumerateSolutions((uid, container)))
            {
                if (name is null)
                    continue;

                if (name.Equals("chemicals"))
                {
                    foreach (var (reagent, quantity) in solution.Comp.Solution.Contents)
                    {
                        if (!component.TriggersReagent.ContainsKey(reagent.Prototype))
                            continue;

                        var tempProt = _prototype.Index<EmotionalTriggersReagentPrototype>(component.TriggersReagent[reagent.Prototype]);

                        _plusEmot += tempProt.PositEffect * hungComp.BaseDecayRate * hungComp.HungerThresholdDecayModifiers[hungComp.LastThreshold];
                        if (tempProt.NegatEffectTime == 0)
                            continue;

                        // The prototype specifies the amount of negative emotions per ounce of substance => we need to distribute the acquisition of this amount,
                        // since substances decrease only by a certain value each second. If we don't do this, it will result
                        // in a crazy stack of negative emotions.
                        var value = tempProt.NegatEffect * hungComp.BaseDecayRate * hungComp.HungerThresholdDecayModifiers[hungComp.LastThreshold];

                        component.NegativeEffects.Add(new List<float> { value, tempProt.NegatEffectTime });
                    }
                }
            }
        }

        // we apply the previously obtained negative emotions from reagents
        if (component.NegativeEffects.Count != 0)
        {
            for (int i = 0; i < component.NegativeEffects.Count; i++)
            {
                if (component.NegativeEffects[i][1] - 1 >= 0)
                {
                    _minEmot += component.NegativeEffects[i][0] / component.NegativeEffects[i][1];
                    component.NegativeEffects[i][0] -= component.NegativeEffects[i][0] / component.NegativeEffects[i][1];
                    component.NegativeEffects[i][1] -= 1;
                }
                else if (component.NegativeEffects[i][0] == 0)
                {
                    component.NegativeEffects.RemoveAt(i);
                }
            }
        }

        var calculatedEmotionalStateThreshold = GetEmotionalStateThreshold(component);
        if (calculatedEmotionalStateThreshold == component.CurrentThreshold)
            return;

        component.CurrentThreshold = calculatedEmotionalStateThreshold;
        DirtyField(uid, component, nameof(EmotionalStateComponent.CurrentThreshold));
        DoEmotionalThresholdEffects(uid, component);
    }

    /// <summary>
    /// This method attempts to deal lethal damage to an entity using a melee weapon.
    /// <param name="uid">The entity that should die</param>
    /// <param name="item">The item that should become the instrument of self-anti-resurrection</param>
    /// <param name="component"></param>
    /// <param name="name">The name of the inventory slot from which to attempt to retrieve the weapon</param>
    private bool TrySuicideUsingMeleeWeapon(EntityUid uid, EntityUid item, EmotionalStateComponent? component, string name = "pocket1")
    {
        if (!_entityManager.TryGetComponent(item, out MeleeWeaponComponent? weaponComp) || weaponComp.Damage.GetTotal() <= 0)
            return false;

        if (!_handSystem.TryPickupAnyHand(uid, item))
        {
            if (!_inventory.TryUnequip(uid: uid, slot: name))
                return false;

            _handSystem.PickupOrDrop(uid, item);
        }

        if (!_entityManager.TryGetComponent(uid, out DamageableComponent? damageComp))
            return false;

        var entitySuicide = new Entity<DamageableComponent>(uid, damageComp);
        _suicideSystem.ApplyLethalDamage(entitySuicide, weaponComp.Damage);

        if (weaponComp.HitSound != null)
        {
            _audioSystem.PlayPvs(weaponComp.HitSound, item);
        }

        return true;
    }

    /// <summary>
    /// This method attempts to deal lethal damage to an entity using a ranged weapon.
    /// Here, the weapon is checked for various components to execute the required logic.
    /// Thus, all ranged weapons can be divided into three types: breech-loading (pistols, lectors, etc.), ballistic (China Lake, shotguns, crossbows, RPGs),
    /// laser and protokinetic (a generally unique type).
    /// <param name="uid">The entity that should die</param>
    /// <param name="item">The item that should become the instrument of self-anti-resurrection</param>
    /// <param name="component"></param>
    /// <param name="name">The name of the inventory slot from which to attempt to retrieve the weapon</param>
    private bool TrySuicideUsingGun(EntityUid uid, EntityUid item, EmotionalStateComponent? component, string name = "pocket1")
    {
        if (!_timing.IsFirstTimePredicted)
            return false;

        if (!_entityManager.TryGetComponent(item, out GunComponent? gunComp))
            return false;

        if (_containerSystem.TryGetContainer(item, "gun_chamber", out var container) && container is ContainerSlot slot)
        {
            if (!TrySuicideUsingChamberGun(uid, item, component, slot, gunComp, name))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        if (_containerSystem.TryGetContainer(item, "ballistic-ammo", out var containerBallistic) && containerBallistic is Container cont)
        {
            if (!TrySuicideUsingBallisticGun(uid, item, component, gunComp, name))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        if (_entityManager.TryGetComponent(item, out BasicEntityAmmoProviderComponent? basicProviderComponent))
        {
            if (basicProviderComponent != null)
            {
                if (!TrySuicideUsingProtoGun(uid, item, (BasicEntityAmmoProviderComponent) basicProviderComponent, gunComp, name))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        if (_entityManager.TryGetComponent(item, out HitscanBatteryAmmoProviderComponent? hitscanProviderComponent))
        {
            if (hitscanProviderComponent != null)
            {
                if (!TrySuicideUsingEnergeGun(uid, item, (HitscanBatteryAmmoProviderComponent) hitscanProviderComponent, gunComp, name))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Method for attempting to deal lethal damage using a laser weapon.
    /// <param name="uid">The entity that should die</param>
    /// <param name="item">The item that should become the instrument of self-anti-resurrection</param>
    /// <param name="component"></param>
    /// <param name="name">The name of the inventory slot from which to attempt to retrieve the weapon</param>
    public bool TrySuicideUsingEnergeGun(EntityUid uid, EntityUid item, HitscanBatteryAmmoProviderComponent hitscanProviderComponent, GunComponent gunComp, string name = "pocket1")
    {
        if (!_handSystem.TryPickupAnyHand(uid, item))
        {
            if (!_inventory.TryUnequip(uid: uid, slot: name))
                return false;

            _handSystem.PickupOrDrop(uid, item);
        }

        var bulletProto = _prototype.Index<HitscanPrototype>(hitscanProviderComponent.Prototype);

        if (bulletProto == null)
            return false;

        if (bulletProto.Damage == null)
            return false;

        if (!_entityManager.TryGetComponent(uid, out DamageableComponent? damageComp))
            return false;

        var entitySuicide = new Entity<DamageableComponent>(uid, damageComp);
        _suicideSystem.ApplyLethalDamage(entitySuicide, bulletProto.Damage);

        if (gunComp.SoundGunshot != null)
            _audioSystem.PlayPvs(gunComp.SoundGunshot, item);

        return true;
    }

    /// <summary>
    /// Method for attempting to deal lethal damage using a protokinetic weapon.
    /// <param name="uid">The entity that should die</param>
    /// <param name="item">The item that should become the instrument of self-anti-resurrection</param>
    /// <param name="component"></param>
    /// <param name="name">The name of the inventory slot from which to attempt to retrieve the weapon</param>
    public bool TrySuicideUsingProtoGun(EntityUid uid, EntityUid item, BasicEntityAmmoProviderComponent basicProviderComponent, GunComponent gunComp, string name = "pocket1")
    {
        if (!_handSystem.TryPickupAnyHand(uid, item))
        {
            if (!_inventory.TryUnequip(uid: uid, slot: name))
                return false;

            _handSystem.PickupOrDrop(uid, item);
        }

        if (!_entityManager.TryGetComponent(uid, out DamageableComponent? damageComp))
            return false;

        if (!_prototype.Index<EntityPrototype>(basicProviderComponent.Proto).Components
                .TryGetValue(Factory.GetComponentName<ProjectileComponent>(), out var projectile))
            return false;

        var p = (ProjectileComponent) projectile.Component;

        if (p != null)
        {
            if (p.Damage != null)
            {
                var entitySuicide = new Entity<DamageableComponent>(uid, damageComp);
                _suicideSystem.ApplyLethalDamage(entitySuicide, p.Damage);

                if (gunComp.SoundGunshot != null)
                    _audioSystem.PlayPvs(gunComp.SoundGunshot, item);
            }
        }

        return false;
    }

    /// <summary>
    /// Method for attempting to deal lethal damage using a breech-loading weapon.
    /// <param name="uid">The entity that should die</param>
    /// <param name="item">The item that should become the instrument of self-anti-resurrection</param>
    /// <param name="component"></param>
    /// <param name="name">The name of the inventory slot from which to attempt to retrieve the weapon</param>
    public bool TrySuicideUsingChamberGun(EntityUid uid, EntityUid item, EmotionalStateComponent? component, ContainerSlot slot, GunComponent gunComp, string name = "pocket1")
    {
        if (!_entityManager.TryGetComponent(item, out ChamberMagazineAmmoProviderComponent? magazineProviderComponent))
            return false;

        if (magazineProviderComponent == null)
            return false;

        if (magazineProviderComponent.BoltClosed != null)
        {
            if (magazineProviderComponent.BoltClosed == false)
            {
                _sharedPopupSystem.PopupEntity(Loc.GetString("an-open-shutter-stops-you"), uid);
                return false;
            }
        }

        if (slot.ContainedEntity == null)
            return false;

        var bullet = (EntityUid) slot.ContainedEntity;

        if (bullet == null)
            return false;

        if (!_entityManager.TryGetComponent(bullet, out DamageOnHighSpeedImpactComponent? damageOnHighSpeedImpactComponent))
            return false;

        if (!_handSystem.TryPickupAnyHand(uid, item))
        {
            if (!_inventory.TryUnequip(uid: uid, slot: name))
                return false;

            _handSystem.PickupOrDrop(uid, item);
        }

        if (!_entityManager.TryGetComponent(bullet, out CartridgeAmmoComponent? cartridgeAmmoComponent))
            return false;

        if (!_entityManager.TryGetComponent(uid, out DamageableComponent? damageComp))
            return false;

        var entitySuicide = new Entity<DamageableComponent>(uid, damageComp);
        _suicideSystem.ApplyLethalDamage(entitySuicide, damageOnHighSpeedImpactComponent.Damage);

        cartridgeAmmoComponent.Spent = true;
        _handSystem.PickupOrDrop(uid, bullet);
        DirtyField(bullet, cartridgeAmmoComponent, nameof(CartridgeAmmoComponent.Spent));
        _sharedAppearanceSystem.SetData(bullet, AmmoVisuals.Spent, true);

        if (gunComp.SoundGunshot != null)
            _audioSystem.PlayPvs(gunComp.SoundGunshot, item);

        return true;
    }

    /// <summary>
    /// Method for attempting to deal lethal damage using a ballistic weapon.
    /// <param name="uid">The entity that should die</param>
    /// <param name="item">The item that should become the instrument of self-anti-resurrection</param>
    /// <param name="component"></param>
    /// <param name="name">The name of the inventory slot from which to attempt to retrieve the weapon</param>
    public bool TrySuicideUsingBallisticGun(EntityUid uid, EntityUid item, EmotionalStateComponent? component, GunComponent gunComp, string name = "pocket1")
    {
        if (!_entityManager.TryGetComponent(item, out BallisticAmmoProviderComponent? ballisticProviderComponent))
            return false;

        EntityUid bullet = new();

        if (ballisticProviderComponent.Entities.Count == 0)
        {
            if (ballisticProviderComponent.UnspawnedCount == 0)
            {
                _sharedPopupSystem.PopupEntity(Loc.GetString("the-lack-of-ammo-saves-you"), uid);
                return false;
            }

            bullet = Spawn(ballisticProviderComponent.Proto, _transform.GetMapCoordinates(uid));
            ballisticProviderComponent.UnspawnedCount--;
            DirtyField(item, ballisticProviderComponent, nameof(BallisticAmmoProviderComponent.UnspawnedCount));
        }
        else
        {
            bullet = ballisticProviderComponent.Entities[0];
        }

        if (bullet == null)
            return false;

        if (!_entityManager.TryGetComponent(bullet, out DamageOnHighSpeedImpactComponent? damageOnHighSpeedImpactComponent))
            return false;

        if (!_handSystem.TryPickupAnyHand(uid, item))
        {
            if (!_inventory.TryUnequip(uid: uid, slot: name))
                return false;

            _handSystem.PickupOrDrop(uid, item);
        }

        if (!_entityManager.TryGetComponent(bullet, out CartridgeAmmoComponent? cartridgeAmmoComponent))
            return false;

        if (!_entityManager.TryGetComponent(uid, out DamageableComponent? damageComp))
            return false;

        var entitySuicide = new Entity<DamageableComponent>(uid, damageComp);
        _suicideSystem.ApplyLethalDamage(entitySuicide, damageOnHighSpeedImpactComponent.Damage);

        cartridgeAmmoComponent.Spent = true;
        _handSystem.PickupOrDrop(uid, bullet);
        DirtyField(bullet, cartridgeAmmoComponent, nameof(CartridgeAmmoComponent.Spent));
        _sharedAppearanceSystem.SetData(bullet, AmmoVisuals.Spent, true);

        if (IsClientSide(bullet))
            Del(bullet);

        if (gunComp.SoundGunshot != null)
            _audioSystem.PlayPvs(gunComp.SoundGunshot, item);

        return true;
    }

    public bool Check(float probability)
    {
        return _random.NextDouble() < probability;
    }

    /// <summary>
    /// Method where slowing effects, shaders, the suicide process, and alert changes should be applied
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <param name="force"></param>
    private void DoEmotionalThresholdEffects(EntityUid uid, EmotionalStateComponent? component, bool force = false)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!Resolve(uid, ref component))
            return;

        // logic for committing suicide when mood is depressive or lower
        if ((component.CurrentThreshold == EmotionalThreshold.Depressive || component.CurrentThreshold == EmotionalThreshold.Demonic)
            && _mobState.IsAlive(uid)
            && _netManager.IsServer
            && Check(component.ChanceOfSuicide))
        {
            // first, we try to find a weapon in the hands
            if (_entityManager.TryGetComponent(uid, out HandsComponent? handsComponent))
            {
                foreach (var hand in handsComponent.Hands.Keys)
                {
                    var tempHoldItem = _handSystem.GetHeldItem(uid, hand);

                    if (tempHoldItem != null)
                    {
                        var holdItem = (EntityUid)tempHoldItem;
                        if (!TrySuicideUsingMeleeWeapon(uid, holdItem, component))
                        {
                            if (!TrySuicideUsingGun(uid, holdItem, component))
                                continue;
                        }

                        component.LastThreshold = component.CurrentThreshold;
                        DirtyField(uid, component, nameof(EmotionalStateComponent.LastThreshold));
                        return;
                    }
                }
            }

            // then we search in pockets, on the belt, on the back, and in the backpack. That is, we check all places where a weapon could be stored.
            var slotEnumerator = _inventory.GetSlotEnumerator(uid);
            while (slotEnumerator.NextItem(out var item, out var slot))
            {
                if (!_entityManager.TryGetComponent(item, out StorageComponent? storageComponent))
                {
                    if (!TrySuicideUsingMeleeWeapon(uid, item, component, slot.Name))
                    {
                        if (!TrySuicideUsingGun(uid, item, component, slot.Name))
                            continue;

                        component.LastThreshold = component.CurrentThreshold;
                        DirtyField(uid, component, nameof(EmotionalStateComponent.LastThreshold));
                        return;
                    }
                }
                else
                {
                    if (storageComponent == null)
                        continue;

                    foreach (var storageItem in storageComponent.StoredItems)
                    {
                        if (!TrySuicideUsingMeleeWeapon(uid, storageItem.Key, component))
                        {
                            if (!TrySuicideUsingGun(uid, storageItem.Key, component))
                                continue;
                        }

                        component.LastThreshold = component.CurrentThreshold;
                        DirtyField(uid, component, nameof(EmotionalStateComponent.LastThreshold));
                        return;
                    }
                }
            }

            // if we didn't find anything on ourselves, we look for something nearby
            _weaponInRange.Clear();
            var Coordinates = _entityManager.GetComponent<TransformComponent>(uid).Coordinates;
            _entityLookup.GetEntitiesInRange(Coordinates, 1, _weaponInRange, flags: LookupFlags.Uncontained);

            foreach (var nearWeapon in _weaponInRange)
            {
                if (nearWeapon == uid)
                    continue;

                if (!TrySuicideUsingMeleeWeapon(uid, nearWeapon, component))
                {
                    if (!TrySuicideUsingGun(uid, nearWeapon, component))
                        continue;
                }

                component.LastThreshold = component.CurrentThreshold;
                DirtyField(uid, component, nameof(EmotionalStateComponent.LastThreshold));
                return;
            }

            var suicideByEnvironmentEvent = new SuicideByEnvironmentEvent(uid);

            // if the humanoid didn't die from using a weapon, we look for special static entities (microwaves, crematorium, etc.)
            var itemQuery = GetEntityQuery<ItemComponent>();
            foreach (var entity in _entityLookup.GetEntitiesInRange(uid, 1, LookupFlags.Approximate | LookupFlags.Static))
            {
                if (itemQuery.HasComponent(entity))
                    continue;

                RaiseLocalEvent(entity, suicideByEnvironmentEvent);
                if (!suicideByEnvironmentEvent.Handled)
                    continue;

                component.LastThreshold = component.CurrentThreshold;
                DirtyField(uid, component, nameof(EmotionalStateComponent.LastThreshold));
                return;
            }
        }

        if (component.CurrentThreshold == component.LastThreshold && !force)
            return;

        // perhaps the implementation of shaders should have been written differently :)
        if (component.CurrentThreshold == EmotionalThreshold.Sad &&
            !_entityManager.TryGetComponent(uid, out SadEmotionOverlayComponent? sadEmotionOverlayComponent))
        {
            RemComp<BlackAndWhiteOverlayComponent>(uid);
            RemComp<DepressiveEmotionOverlayComponent>(uid);
            AddComp<SadEmotionOverlayComponent>(uid);
        }
        else if (component.CurrentThreshold == EmotionalThreshold.Depressive &&
            !_entityManager.TryGetComponent(uid, out DepressiveEmotionOverlayComponent? depressiveOverlayComponent))
        {
            RemComp<SadEmotionOverlayComponent>(uid);
            RemComp<BlackAndWhiteOverlayComponent>(uid);
            AddComp<DepressiveEmotionOverlayComponent>(uid);
        }
        else if (component.CurrentThreshold == EmotionalThreshold.Demonic &&
            !_entityManager.TryGetComponent(uid, out BlackAndWhiteOverlayComponent? blackAndWhiteOverlayComponent))
        {
            RemComp<SadEmotionOverlayComponent>(uid);
            RemComp<DepressiveEmotionOverlayComponent>(uid);
            AddComp<BlackAndWhiteOverlayComponent>(uid);
        }
        else
        {
            RemComp<SadEmotionOverlayComponent>(uid);
            RemComp<DepressiveEmotionOverlayComponent>(uid);
            RemComp<BlackAndWhiteOverlayComponent>(uid);
        }

        // update movementspeed
        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);

        // updating the interface icon
        if (component.EmotionalThresholdAlerts.TryGetValue(component.CurrentThreshold, out var alertId))
        {
            _alerts.ShowAlert(uid, alertId);
        }
        else
        {
            _alerts.ClearAlertCategory(uid, component.EmotionalAlertCategory);
        }

        component.LastThreshold = component.CurrentThreshold;
        DirtyField(uid, component, nameof(EmotionalStateComponent.LastThreshold));
        return;
    }

    /// <summary>
    /// Method for getting the current state from the <see cref="HungerComponent.Thresholds"/> enumeration
    /// based on the current number of emotional state points
    /// </summary>
    /// <param name="component"></param>
    /// <param name="currentValue"></param>
    public EmotionalThreshold GetEmotionalStateThreshold(EmotionalStateComponent component, float? currentValue = null)
    {
        currentValue ??= GetEmotionalState(component);
        var result = EmotionalThreshold.Demonic;
        var value = component.Thresholds[EmotionalThreshold.Rainbow];
        foreach (var threshold in component.Thresholds)
        {
            if (threshold.Value <= value && threshold.Value >= currentValue)
            {
                result = threshold.Key;
                value = threshold.Value;
            }
        }

        return result;
    }

    /// <summary>
    /// Method for getting the current emotional state icon
    /// </summary>
    public bool TryGetStatusIconPrototype(EmotionalStateComponent component, [NotNullWhen(true)] out EmotionalStateIconPrototype? prototype)
    {
        prototype = null;
        _prototype.TryIndex(component.StatusIconsThresholds[component.CurrentThreshold], out prototype);

        return prototype != null;
    }

    /// <summary>
    /// Method for getting the current emotional state points gain icon.
    /// Needed by the psychologist to identify patient triggers through the visor.
    /// </summary>
    public bool TryGetDeltaIconPrototype(EmotionalStateComponent component, [NotNullWhen(true)] out EmotionalStateIconPrototype? prototype)
    {
        var deltaValue = component.LastDeltaAuthoritativeEmotionalValue;
        if (deltaValue == 0)
        {
            _prototype.TryIndex(EmotionalStateIconZerolId, out prototype);
            return prototype != null;
        }

        if (deltaValue > 0 && deltaValue <= 5)
        {
            _prototype.TryIndex(EmotionalStateIconPlus1Id, out prototype);
            return prototype != null;
        }

        if (deltaValue > 5 && deltaValue <= 10)
        {
            _prototype.TryIndex(EmotionalStateIconPlus2Id, out prototype);
            return prototype != null;
        }

        if (deltaValue > 10)
        {
            _prototype.TryIndex(EmotionalStateIconPlus3Id, out prototype);
            return prototype != null;
        }

        if (deltaValue < 0 && deltaValue >= -5)
        {
            _prototype.TryIndex(EmotionalStateIconMinus1Id, out prototype);
            return prototype != null;
        }

        if (deltaValue < -5 && deltaValue >= -10)
        {
            _prototype.TryIndex(EmotionalStateIconMinus2Id, out prototype);
            return prototype != null;
        }

        if (deltaValue < -10)
        {
            _prototype.TryIndex(EmotionalStateIconMinus3Id, out prototype);
            return prototype != null;
        }

        prototype = null;
        return prototype != null;
    }

    /// <summary>
    /// Returns the passed value within the range of 0.0f to 1000.0f
    /// (currently these are the emotional state boundaries)
    /// </summary>
    private static float ClampEmotionalStateValueWithinThresholds(EmotionalStateComponent component, float emotionalStateValue)
    {
        return Math.Clamp(emotionalStateValue,
            component.Thresholds[EmotionalThreshold.Demonic],
            component.Thresholds[EmotionalThreshold.Rainbow]);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<EmotionalStateComponent>();
        while (query.MoveNext(out var uid, out var emotionalState))
        {
            if (_timing.CurTime < emotionalState.NextThresholdUpdateTime)
                continue;

            emotionalState.NextThresholdUpdateTime = _timing.CurTime + emotionalState.ThresholdUpdateRate;

            UpdateCurrentThreshold(uid, emotionalState);
            DoEmotionalThresholdEffects(uid, emotionalState);
        }
    }
}
