using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Disease.Components;
using Content.Server.DoAfter;
using Content.Server.MobState;
using Content.Server.Nutrition.EntitySystems;
using Content.Server.Popups;
using Content.Shared.Clothing.Components;
using Content.Shared.Disease;
using Content.Shared.Disease.Components;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.MobState.Components;
using Content.Shared.Rejuvenate;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Utility;
using System.Threading;

namespace Content.Server.Disease
{

    /// <summary>
    /// Handles disease propagation & curing
    /// </summary>
    public sealed class DiseaseSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly ISerializationManager _serializationManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
        [Dependency] private readonly InventorySystem _inventorySystem = default!;
        [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DiseaseCarrierComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<DiseaseCarrierComponent, CureDiseaseAttemptEvent>(OnTryCureDisease);
            SubscribeLocalEvent<DiseaseCarrierComponent, RejuvenateEvent>(OnRejuvenate);
            SubscribeLocalEvent<DiseasedComponent, ContactInteractionEvent>(OnContactInteraction);
            SubscribeLocalEvent<DiseasedComponent, EntitySpokeEvent>(OnEntitySpeak);
            SubscribeLocalEvent<DiseaseProtectionComponent, GotEquippedEvent>(OnEquipped);
            SubscribeLocalEvent<DiseaseProtectionComponent, GotUnequippedEvent>(OnUnequipped);
            SubscribeLocalEvent<DiseaseVaccineComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<DiseaseVaccineComponent, ExaminedEvent>(OnExamined);
            // Handling stuff from other systems
            SubscribeLocalEvent<DiseaseCarrierComponent, ApplyMetabolicMultiplierEvent>(OnApplyMetabolicMultiplier);
            // Private events stuff
            SubscribeLocalEvent<TargetVaxxSuccessfulEvent>(OnTargetVaxxSuccessful);
            SubscribeLocalEvent<VaxxCancelledEvent>(OnVaxxCancelled);
        }

        private Queue<EntityUid> AddQueue = new();
        private Queue<(DiseaseCarrierComponent carrier, DiseasePrototype disease)> CureQueue = new();

        /// <summary>
        /// First, adds or removes diseased component from the queues and clears them.
        /// Then, iterates over every diseased component to check for their effects
        /// and cures
        /// </summary>
        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var entity in AddQueue)
            {
                EnsureComp<DiseasedComponent>(entity);
            }

            AddQueue.Clear();

            foreach (var tuple in CureQueue)
            {
                if (tuple.carrier.Diseases.Count == 1) //This is reliable unlike testing Count == 0 right after removal for reasons I don't quite get
                    RemComp<DiseasedComponent>(tuple.carrier.Owner);
                tuple.carrier.PastDiseases.Add(tuple.disease);
                tuple.carrier.Diseases.Remove(tuple.disease);
            }
            CureQueue.Clear();

            foreach (var (_, carrierComp, mobState) in EntityQuery<DiseasedComponent, DiseaseCarrierComponent, MobStateComponent>())
            {
                DebugTools.Assert(carrierComp.Diseases.Count > 0);

                if (_mobStateSystem.IsDead(mobState.Owner, mobState))
                {
                    if (_random.Prob(0.005f * frameTime)) //Mean time to remove is 200 seconds per disease
                        CureDisease(carrierComp, _random.Pick(carrierComp.Diseases));

                    continue;
                }

                for (var i = 0; i < carrierComp.Diseases.Count; i++) //this is a for-loop so that it doesn't break when new diseases are added
                {
                    var disease = carrierComp.Diseases[i];
                    disease.Accumulator += frameTime;
                    disease.TotalAccumulator += frameTime;

                    if (disease.Accumulator < disease.TickTime) continue;

                    // if the disease is on the silent disease list, don't do effects
                    var doEffects = carrierComp.CarrierDiseases?.Contains(disease.ID) != true;
                    var args = new DiseaseEffectArgs(carrierComp.Owner, disease, EntityManager);
                    disease.Accumulator -= disease.TickTime;

                    int stage = 0; //defaults to stage 0 because you should always have one
                    float lastThreshold = 0;
                    for (var j = 0; j < disease.Stages.Count; j++)
                    {
                        if (disease.TotalAccumulator >= disease.Stages[j] &&
                            disease.Stages[j] > lastThreshold)
                        {
                            lastThreshold = disease.Stages[j];
                            stage = j;
                        }
                    }

                    foreach (var cure in disease.Cures)
                    {
                        if (cure.Stages.AsSpan().Contains(stage) && cure.Cure(args))
                            CureDisease(carrierComp, disease);
                    }

                    if (doEffects)
                    {
                        foreach (var effect in disease.Effects)
                        {
                            if (effect.Stages.AsSpan().Contains(stage) && _random.Prob(effect.Probability))
                                effect.Effect(args);
                        }
                    }
                }
            }
        }

        ///
        /// Event Handlers
        ///

        /// <summary>
        /// Fill in the natural immunities of this entity.
        /// </summary>
        private void OnInit(EntityUid uid, DiseaseCarrierComponent component, ComponentInit args)
        {
            if (component.NaturalImmunities == null || component.NaturalImmunities.Count == 0)
                return;

            foreach (var immunity in component.NaturalImmunities)
            {
                if (_prototypeManager.TryIndex<DiseasePrototype>(immunity, out var disease))
                    component.PastDiseases.Add(disease);
                else
                {
                    Logger.Error("Failed to index disease prototype + " + immunity + " for " + uid);
                }
            }
        }

        /// <summary>
        /// Used when something is trying to cure ANY disease on the target,
        /// not for special disease interactions. Randomly
        /// tries to cure every disease on the target.
        /// </summary>
        private void OnTryCureDisease(EntityUid uid, DiseaseCarrierComponent component, CureDiseaseAttemptEvent args)
        {
            foreach (var disease in component.Diseases)
            {
                var cureProb = ((args.CureChance / component.Diseases.Count) - disease.CureResist);
                if (cureProb < 0)
                    return;
                if (cureProb > 1)
                {
                    CureDisease(component, disease);
                    return;
                }
                if (_random.Prob(cureProb))
                {
                    CureDisease(component, disease);
                    return;
                }
            }
        }

        private void OnRejuvenate(EntityUid uid, DiseaseCarrierComponent component, RejuvenateEvent args)
        {
            CureAllDiseases(uid, component);
        }

        /// <summary>
        /// Called when a component with disease protection
        /// is equipped so it can be added to the person's
        /// total disease resistance
        /// </summary>
        private void OnEquipped(EntityUid uid, DiseaseProtectionComponent component, GotEquippedEvent args)
        {
            // This only works on clothing
            if (!TryComp<ClothingComponent>(uid, out var clothing))
                return;
            // Is the clothing in its actual slot?
            if (!clothing.Slots.HasFlag(args.SlotFlags))
                return;
            // Give the user the component's disease resist
            if(TryComp<DiseaseCarrierComponent>(args.Equipee, out var carrier))
                carrier.DiseaseResist += component.Protection;
            // Set the component to active to the unequip check isn't CBT
            component.IsActive = true;
        }

        /// <summary>
        /// Called when a component with disease protection
        /// is unequipped so it can be removed from the person's
        /// total disease resistance
        /// </summary>
        private void OnUnequipped(EntityUid uid, DiseaseProtectionComponent component, GotUnequippedEvent args)
        {
            // Only undo the resistance if it was affecting the user
            if (!component.IsActive)
                return;
            if(TryComp<DiseaseCarrierComponent>(args.Equipee, out var carrier))
                carrier.DiseaseResist -= component.Protection;
            component.IsActive = false;
        }

        /// <summary>
        /// Called when it's already decided a disease will be cured
        /// so it can be safely queued up to be removed from the target
        /// and added to past disease history (for immunity)
        /// </summary>
        private void CureDisease(DiseaseCarrierComponent carrier, DiseasePrototype disease)
        {
            var CureTuple = (carrier, disease);
            CureQueue.Enqueue(CureTuple);
            _popupSystem.PopupEntity(Loc.GetString("disease-cured"), carrier.Owner, Filter.Entities(carrier.Owner));
        }

        public void CureAllDiseases(EntityUid uid, DiseaseCarrierComponent? carrier = null)
        {
            if (!Resolve(uid, ref carrier))
                return;

            foreach (var disease in carrier.Diseases)
            {
                CureDisease(carrier, disease);
            }
        }

        /// <summary>
        /// When a diseased person interacts with something, check infection.
        /// </summary>
        private void OnContactInteraction(EntityUid uid, DiseasedComponent component, ContactInteractionEvent args)
        {
            InteractWithDiseased(uid, args.Other);
        }

        private void OnEntitySpeak(EntityUid uid, DiseasedComponent component, EntitySpokeEvent args)
        {
            if (TryComp<DiseaseCarrierComponent>(uid, out var carrier))
            {
                SneezeCough(uid, _random.Pick(carrier.Diseases), string.Empty);
            }
        }

        /// <summary>
        /// Called when a vaccine is used on someone
        /// to handle the vaccination doafter
        /// </summary>
        private void OnAfterInteract(EntityUid uid, DiseaseVaccineComponent vaxx, AfterInteractEvent args)
        {
            if (vaxx.CancelToken != null)
            {
                vaxx.CancelToken.Cancel();
                vaxx.CancelToken = null;
                return;
            }
            if (args.Target == null)
                return;

            if (!args.CanReach)
                return;

            if (vaxx.CancelToken != null)
                return;

            if (!TryComp<DiseaseCarrierComponent>(args.Target, out var carrier))
                return;

            if (vaxx.Used)
            {
                _popupSystem.PopupEntity(Loc.GetString("vaxx-already-used"), args.User, Filter.Entities(args.User));
                return;
            }

            vaxx.CancelToken = new CancellationTokenSource();
            _doAfterSystem.DoAfter(new DoAfterEventArgs(args.User, vaxx.InjectDelay, vaxx.CancelToken.Token, target: args.Target)
            {
                BroadcastFinishedEvent = new TargetVaxxSuccessfulEvent(args.User, args.Target, vaxx, carrier),
                BroadcastCancelledEvent = new VaxxCancelledEvent(vaxx),
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnStun = true,
                NeedHand = true
            });
        }

        /// <summary>
        /// Called when a vaccine is examined.
        /// Currently doesn't do much because
        /// vaccines don't have unique art with a seperate
        /// state visualizer.
        /// </summary>
        private void OnExamined(EntityUid uid, DiseaseVaccineComponent vaxx, ExaminedEvent args)
        {
            if (args.IsInDetailsRange)
            {
                if (vaxx.Used)
                    args.PushMarkup(Loc.GetString("vaxx-used"));
                else
                    args.PushMarkup(Loc.GetString("vaxx-unused"));
            }
        }


    private void OnApplyMetabolicMultiplier(EntityUid uid, DiseaseCarrierComponent component, ApplyMetabolicMultiplierEvent args)
    {
        if (args.Apply)
        {
            foreach (var disease in component.Diseases)
            {
                disease.TickTime *= args.Multiplier;
                return;
            }
        }
        foreach (var disease in component.Diseases)
        {
            disease.TickTime /= args.Multiplier;
            if (disease.Accumulator >= disease.TickTime)
                disease.Accumulator = disease.TickTime;
        }
    }


        ///
        /// Helper functions
        ///

        /// <summary>
        /// Tries to infect anyone that
        /// interacts with a diseased person or body
        /// </summary>
        private void InteractWithDiseased(EntityUid diseased, EntityUid target, DiseaseCarrierComponent? diseasedCarrier = null)
        {
            if (!Resolve(diseased, ref diseasedCarrier, false) ||
                diseasedCarrier.Diseases.Count == 0 ||
                !TryComp<DiseaseCarrierComponent>(target, out var carrier))
                return;

            var disease = _random.Pick(diseasedCarrier.Diseases);
            TryInfect(carrier, disease, 0.4f);
        }

        /// <summary>
        /// Adds a disease to a target
        /// if it's not already in their current
        /// or past diseases. If you want this
        /// to not be guaranteed you are looking
        /// for TryInfect.
        /// </summary>
        public void TryAddDisease(EntityUid host, DiseasePrototype addedDisease, DiseaseCarrierComponent? target = null)
        {
            if (!Resolve(host, ref target, false))
                return;

            foreach (var disease in target.AllDiseases)
            {
                if (disease.ID == addedDisease?.ID) //ID because of the way protoypes work
                    return;
            }

            var freshDisease = _serializationManager.Copy(addedDisease);

            if (freshDisease == null) return;

            target.Diseases.Add(freshDisease);
            AddQueue.Enqueue(target.Owner);
        }

        public void TryAddDisease(EntityUid host, string? addedDisease, DiseaseCarrierComponent? target = null)
        {
            if (addedDisease == null || !_prototypeManager.TryIndex<DiseasePrototype>(addedDisease, out var added))
                return;

            TryAddDisease(host, added, target);
        }

        /// <summary>
        /// Pits the infection chance against the
        /// person's disease resistance and
        /// rolls the dice to see if they get
        /// the disease.
        /// </summary>
        /// <param name="carrier">The target of the disease</param>
        /// <param name="disease">The disease to apply</param>
        /// <param name="chance">% chance of the disease being applied, before considering resistance</param>
        /// <param name="forced">Bypass the disease's infectious trait.</param>
        public void TryInfect(DiseaseCarrierComponent carrier, DiseasePrototype? disease, float chance = 0.7f, bool forced = false)
        {
            if(disease == null || !forced && !disease.Infectious)
                return;
            var infectionChance = chance - carrier.DiseaseResist;
            if (infectionChance <= 0)
                return;
            if (_random.Prob(infectionChance))
                TryAddDisease(carrier.Owner, disease, carrier);
        }

        public void TryInfect(DiseaseCarrierComponent carrier, string? disease, float chance = 0.7f, bool forced = false)
        {
            if (disease == null || !_prototypeManager.TryIndex<DiseasePrototype>(disease, out var d))
                return;

            TryInfect(carrier, d, chance, forced);
        }

        /// <summary>
        /// Plays a sneeze/cough popup if applicable
        /// and then tries to infect anyone in range
        /// if the snougher is not wearing a mask.
        /// </summary>
        public void SneezeCough(EntityUid uid, DiseasePrototype? disease, string snoughMessage, bool airTransmit = true, TransformComponent? xform = null)
        {
            if (!Resolve(uid, ref xform)) return;

            if (!string.IsNullOrEmpty(snoughMessage))
                _popupSystem.PopupEntity(Loc.GetString(snoughMessage, ("person", Identity.Entity(uid, EntityManager))), uid, Filter.Pvs(uid));

            if (disease is not { Infectious: true } || !airTransmit)
                return;

            if (_inventorySystem.TryGetSlotEntity(uid, "mask", out var maskUid) &&
                EntityManager.TryGetComponent<IngestionBlockerComponent>(maskUid, out var blocker) &&
                blocker.Enabled)
                return;

            var carrierQuery = GetEntityQuery<DiseaseCarrierComponent>();

            foreach (var entity in _lookup.GetEntitiesInRange(xform.MapPosition, 2f))
            {
                if (!carrierQuery.TryGetComponent(entity, out var carrier) ||
                    !_interactionSystem.InRangeUnobstructed(uid, entity)) continue;

                TryInfect(carrier, disease, 0.3f);
            }
        }

        /// <summary>
        /// Adds a disease to the carrier's
        /// past diseases to give them immunity
        /// IF they don't already have the disease.
        /// </summary>
        public void Vaccinate(DiseaseCarrierComponent carrier, DiseasePrototype disease)
        {
            foreach (var currentDisease in carrier.Diseases)
            {
                if (currentDisease.ID == disease.ID) //ID because of the way protoypes work
                    return;
            }
            carrier.PastDiseases.Add(disease);
        }

        ///
        /// Private Events Stuff
        ///

        /// <summary>
        /// Injects the vaccine into the target
        /// if the doafter is completed
        /// </summary>
        private void OnTargetVaxxSuccessful(TargetVaxxSuccessfulEvent args)
        {
            if (args.Vaxx.Disease == null)
                return;
            Vaccinate(args.Carrier, args.Vaxx.Disease);
            EntityManager.DeleteEntity(args.Vaxx.Owner);
        }

        /// <summary>
        /// Cancels the vaccine doafter
        /// </summary>
        private static void OnVaxxCancelled(VaxxCancelledEvent args)
        {
            args.Vaxx.CancelToken = null;
        }
        /// These two are standard doafter stuff you can ignore
        private sealed class VaxxCancelledEvent : EntityEventArgs
        {
            public readonly DiseaseVaccineComponent Vaxx;
            public VaxxCancelledEvent(DiseaseVaccineComponent vaxx)
            {
                Vaxx = vaxx;
            }
        }
        private sealed class TargetVaxxSuccessfulEvent : EntityEventArgs
        {
            public EntityUid User { get; }
            public EntityUid? Target { get; }
            public DiseaseVaccineComponent Vaxx { get; }
            public DiseaseCarrierComponent Carrier { get; }
            public TargetVaxxSuccessfulEvent(EntityUid user, EntityUid? target, DiseaseVaccineComponent vaxx, DiseaseCarrierComponent carrier)
            {
                User = user;
                Target = target;
                Vaxx = vaxx;
                Carrier = carrier;
            }
        }
    }

        /// <summary>
        /// This event is fired by chems
        /// and other brute-force rather than
        /// specific cures. It will roll the dice to attempt
        /// to cure each disease on the target
        /// </summary>
    public sealed class CureDiseaseAttemptEvent : EntityEventArgs
    {
        public float CureChance { get; }
        public CureDiseaseAttemptEvent(float cureChance)
        {
            CureChance = cureChance;
        }
    }

    /// <summary>
    /// Controls whether the snough is a sneeze, cough
    /// or neither. If none, will not create
    /// a popup. Mostly used for talking
    /// </summary>
    public enum SneezeCoughType
    {
        Sneeze,
        Cough,
        None
    }
}
