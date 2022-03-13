using System.Threading;
using Content.Shared.Disease;
using Content.Shared.Disease.Components;
using Content.Server.Disease.Components;
using Content.Server.Clothing.Components;
using Content.Shared.MobState.Components;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Interaction;
using Content.Server.Popups;
using Content.Server.DoAfter;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;
using Content.Shared.Inventory.Events;
using Content.Server.Nutrition.EntitySystems;

namespace Content.Server.Disease
{
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

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DiseaseCarrierComponent, CureDiseaseAttemptEvent>(OnTryCureDisease);
            SubscribeLocalEvent<DiseasedComponent, InteractHandEvent>(OnInteractDiseasedHand);
            SubscribeLocalEvent<DiseasedComponent, InteractUsingEvent>(OnInteractDiseasedUsing);
            SubscribeLocalEvent<DiseaseProtectionComponent, GotEquippedEvent>(OnEquipped);
            SubscribeLocalEvent<DiseaseProtectionComponent, GotUnequippedEvent>(OnUnequipped);
            SubscribeLocalEvent<DiseaseVaccineComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<DiseaseVaccineComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<TargetVaxxSuccessfulEvent>(OnTargetVaxxSuccessful);
            SubscribeLocalEvent<VaxxCancelledEvent>(OnVaxxCancelled);
        }

        private Queue<EntityUid> AddQueue = new();
        private Queue<(DiseaseCarrierComponent, DiseasePrototype)> CureQueue = new();
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
                if (tuple.Item1.Diseases.Count == 1) //This is reliable unlike testing Count == 0 right after removal for reasons I don't quite get
                    RemComp<DiseasedComponent>(tuple.Item1.Owner);
                tuple.Item1.PastDiseases.Add(tuple.Item2);
                tuple.Item1.Diseases.Remove(tuple.Item2);
            }
            CureQueue.Clear();

            foreach (var (diseasedComp, carrierComp, mobState) in EntityQuery<DiseasedComponent, DiseaseCarrierComponent, MobStateComponent>(false))
            {
                if (mobState.IsDead())
                {
                    if (_random.Prob(0.005f * frameTime)) //Mean time to remove is 200 seconds per disease
                    {
                        CureDisease(carrierComp, _random.Pick(carrierComp.Diseases));
                    }
                    continue;
                }

                foreach(var disease in carrierComp.Diseases)
                {
                    var args = new DiseaseEffectArgs(carrierComp.Owner, disease, EntityManager);
                    disease.Accumulator += frameTime;
                    if (disease.Accumulator >= 1f)
                    {
                        disease.Accumulator -= 1f;
                        foreach (var cure in disease.Cures)
                            if (cure.Cure(args))
                            {
                                CureDisease(carrierComp, disease);
                            }
                        foreach (var effect in disease.Effects)
                            if (_random.Prob(effect.Probability))
                                effect.Effect(args);
                    }
                }
            }
        }
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

        private void OnEquipped(EntityUid uid, DiseaseProtectionComponent component, GotEquippedEvent args)
        {
            if (TryComp<ClothingComponent>(uid, out var clothing))
                if (clothing.SlotFlags != args.SlotFlags)
                    return;

            if(TryComp<DiseaseCarrierComponent>(args.Equipee, out var carrier))
                carrier.DiseaseResist += component.Protection;
        }


        private void OnUnequipped(EntityUid uid, DiseaseProtectionComponent component, GotUnequippedEvent args)
        {
            if(TryComp<DiseaseCarrierComponent>(args.Equipee, out var carrier))
                carrier.DiseaseResist -= component.Protection;
        }
        private void CureDisease(DiseaseCarrierComponent carrier, DiseasePrototype? disease)
        {
            if (disease == null)
                return;
            var CureTuple = (carrier, disease);
            CureQueue.Enqueue(CureTuple);
            _popupSystem.PopupEntity(Loc.GetString("disease-cured"), carrier.Owner, Filter.Entities(carrier.Owner));
        }

        private void OnInteractDiseasedHand(EntityUid uid, DiseasedComponent component, InteractHandEvent args)
        {
            if (!_interactionSystem.InRangeUnobstructed(args.User, args.Target))
                return;

            InteractWithDiseased (args.Target, args.User);
        }

        private void OnInteractDiseasedUsing(EntityUid uid, DiseasedComponent component, InteractUsingEvent args)
        {
            InteractWithDiseased(args.Target, args.User);
        }

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

        private void OnExamined(EntityUid uid, DiseaseVaccineComponent vaxx, ExaminedEvent args)
        {
            if (args.IsInDetailsRange)
            {
                if (vaxx.Used)
                {
                    args.PushMarkup(Loc.GetString("vaxx-used"));
                }
                else
                {
                    args.PushMarkup(Loc.GetString("vaxx-unused"));
                }
            }
        }

        private void InteractWithDiseased(EntityUid diseased, EntityUid target)
        {
            if (!TryComp<DiseaseCarrierComponent>(target, out var carrier))
                return;

            var disease = _random.Pick(Comp<DiseaseCarrierComponent>(diseased).Diseases);
            if (disease != null)
                TryInfect(carrier, disease, 0.4f);
        }
        public void TryAddDisease(DiseaseCarrierComponent? target, DiseasePrototype? addedDisease, string? diseaseName = null, EntityUid host = default!)
        {
            if (diseaseName != null && _prototypeManager.TryIndex(diseaseName, out DiseasePrototype? diseaseProto))
                addedDisease = diseaseProto;

            if (host != default!)
                target = Comp<DiseaseCarrierComponent>(host);

            if (target != null)
            {
                foreach (var disease in target.AllDiseases)
                {
                    if (disease.Name == addedDisease?.Name) //Name because of the way protoypes work
                        return;
                }
            var freshDisease = _serializationManager.CreateCopy(addedDisease) ?? default!;
            target.Diseases.Add(freshDisease);
            AddQueue.Enqueue(target.Owner);
            }
        }
        public void TryInfect(DiseaseCarrierComponent carrier, DiseasePrototype? disease, float chance = 0.7f)
        {
            if(disease == null || !disease.Infectious)
                return;
            var infectionChance = chance - carrier.DiseaseResist;
            if (infectionChance <= 0)
                return;
            if (_random.Prob(infectionChance))
                TryAddDisease(carrier, disease);
        }
        public void SneezeCough(EntityUid uid, DiseasePrototype? disease, SneezeCoughType Snough, float infectionChance = 0.3f)
        {
            IngestionBlockerComponent blocker;

            var xform = Comp<TransformComponent>(uid);

            if (Snough == SneezeCoughType.Sneeze)
                _popupSystem.PopupEntity(Loc.GetString("disease-sneeze", ("person", uid)), uid, Filter.Pvs(uid));
            else if (Snough == SneezeCoughType.Cough)
                _popupSystem.PopupEntity(Loc.GetString("disease-cough", ("person", uid)), uid, Filter.Pvs(uid));


            if (disease == null || !disease.Infectious)
                return;

            if (_inventorySystem.TryGetSlotEntity(uid, "mask", out var maskUid) &&
                EntityManager.TryGetComponent(maskUid, out blocker) &&
                blocker.Enabled)
                return;

            foreach (var entity in _lookup.GetEntitiesInRange(xform.MapID, xform.WorldPosition, infectionChance))
            {
                if (TryComp<DiseaseCarrierComponent>(entity, out var carrier))
                    TryInfect(carrier, disease, 0.3f);
            }

        }

        public void Vaccinate(DiseaseCarrierComponent carrier, DiseasePrototype disease)
        {
            foreach (var currentDisease in carrier.Diseases)
                {
                    if (currentDisease.Name == disease.Name) //Name because of the way protoypes work
                        return;
                }
            carrier.PastDiseases.Add(disease);
        }

        private void OnTargetVaxxSuccessful(TargetVaxxSuccessfulEvent args)
        {
            if (args.Vaxx.Disease == null)
                return;
            Vaccinate(args.Carrier, args.Vaxx.Disease);
        }

        private static void OnVaxxCancelled(VaxxCancelledEvent args)
        {
            args.Vaxx.CancelToken = null;
        }
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
        public sealed class CureDiseaseAttemptEvent : EntityEventArgs
        {
            public float CureChance { get; }

            public CureDiseaseAttemptEvent(float cureChance)
            {
                CureChance = cureChance;
            }
        }

        public enum SneezeCoughType
        {
            Sneeze,

            Cough,
            None
        }
}
