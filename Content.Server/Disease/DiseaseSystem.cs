using System.Linq;
using Content.Shared.Disease;
using Content.Shared.Interaction;
using Content.Server.Popups;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;

namespace Content.Server.Disease
{
    public sealed class DiseaseSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly ISerializationManager _serializationManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DiseaseCarrierComponent, CureDiseaseAttemptEvent>(OnTryCureDisease);
            SubscribeLocalEvent<DiseaseInteractSourceComponent, AfterInteractEvent>(OnAfterInteract);
        }
        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var (diseasedComp, carrierComp) in EntityQuery<DiseasedComponent, DiseaseCarrierComponent>(false).ToArray())
            {
                if (carrierComp.Diseases.Count > 0)
                {
                    foreach(var disease in carrierComp.Diseases)
                    {
                        var args = new DiseaseEffectArgs(carrierComp.Owner, disease);
                        disease.Accumulator += frameTime;
                        if (disease.Accumulator >= 1f)
                        {
                            disease.Accumulator -= 1f;
                            foreach (var cure in disease.Cures)
                                if (cure.Cure(args))
                                {
                                    CureDisease(carrierComp, disease);
                                    return; // Get the hell out before we trigger enumeration errors, sorry you can only cure like 30 diseases a second
                                }
                            foreach (var effect in disease.Effects)
                            if (_random.Prob(effect.Probability))
                                effect.Effect(args);
                        }
                    }
                if (carrierComp.Diseases.Count == 0)
                  RemComp<DiseasedComponent>(diseasedComp.Owner);
                }
            }
        }

        private void CureDisease(DiseaseCarrierComponent carrier, DiseasePrototype? disease)
        {
            if (disease == null)
                return;
            carrier.Diseases.Remove(disease);
            _popupSystem.PopupEntity(Loc.GetString("disease-cured"), carrier.Owner, Filter.Pvs(carrier.Owner));
        }
        private void TryAddDisease(DiseaseCarrierComponent target, DiseasePrototype addedDisease)
        {
            if (target.Diseases.Count > 0)
            {
                foreach (var disease in target.Diseases)
                {
                    if (disease.Name == addedDisease.Name)
                        return;
                }
            }
            var freshDisease = _serializationManager.CreateCopy(addedDisease) ?? default!;
            target.Diseases.Add(freshDisease);
            EnsureComp<DiseasedComponent>(target.Owner);
        }

        private void OnAfterInteract(EntityUid uid, DiseaseInteractSourceComponent component, AfterInteractEvent args)
        {
            if (!args.CanReach || args.Target == null)
                return;

            if (!_prototypeManager.TryIndex(component.Disease, out DiseasePrototype? compDisease))
                return;

            if (!TryComp<DiseaseCarrierComponent>(args.Target, out var targetDiseases) || targetDiseases == null)
                return;
            TryAddDisease(targetDiseases, compDisease);
        }

        private async void OnTryCureDisease(EntityUid uid, DiseaseCarrierComponent component, CureDiseaseAttemptEvent args)
        {
            if (component.Diseases.Count == 0)
                return;
            foreach (var disease in component.Diseases)
            {
                if (_random.Prob((args.CureChance / component.Diseases.Count) - disease.CureResist))
                {
                    CureDisease(component, disease);
                    return;
                }
            }
        }

        private void TryInfect(DiseaseCarrierComponent carrier, DiseasePrototype? disease, float chance = 0.7f)
        {
            if(disease == null)
                return;

            if (_random.Prob(chance))
                TryAddDisease(carrier, disease);
        }
        public void SneezeCough(EntityUid uid, DiseasePrototype? disease, SneezeCoughType Snough)
        {
            var xform = Comp<TransformComponent>(uid);
            if (Snough == SneezeCoughType.Sneeze)
                _popupSystem.PopupEntity(Loc.GetString("disease-sneeze", ("person", uid)), uid, Filter.Pvs(uid));
            else
                _popupSystem.PopupEntity(Loc.GetString("disease-cough", ("person", uid)), uid, Filter.Pvs(uid));

            foreach (var entity in _lookup.GetEntitiesInRange(xform.MapID, xform.WorldPosition, 1.5f))
            {
                if (TryComp<DiseaseCarrierComponent>(entity, out var carrier))
                    TryInfect(carrier, disease);
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

            Cough
        }
}
