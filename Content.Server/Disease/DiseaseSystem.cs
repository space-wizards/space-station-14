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

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DiseaseCarrierComponent, CureDiseaseAttemptEvent>(OnTryCureDisease);
            SubscribeLocalEvent<DiseaseInteractSourceComponent, AfterInteractEvent>(OnAfterInteract);
        }
        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var (diseasedComp, carrierComp) in EntityQuery<DiseasedComponent, DiseaseCarrierComponent>(false))
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
            if (args.TargetSpecificDisease && args.SpecificDisease != null && _prototypeManager.TryIndex(args.SpecificDisease, out DiseasePrototype? specificDisease))
            {
                foreach (var disease in component.Diseases)
                {
                    if (disease.Name == args.SpecificDisease && _random.Prob(args.CureChance - disease.CureResist))
                    {
                        CureDisease(component, disease);
                        return;
                    }
                }
                var firstDisease = component.Diseases[0];
                if (_random.Prob(args.CureChance - firstDisease.CureResist))
                {
                    CureDisease(component, firstDisease);
                }
            }
        }
    }
        public sealed class CureDiseaseAttemptEvent : EntityEventArgs
        {
            public bool TargetSpecificDisease { get; }

            public string? SpecificDisease { get; }

            public float CureChance { get; }

            public CureDiseaseAttemptEvent(bool targetSpecificDisease, string? specificDisease, float cureChance)
            {
                TargetSpecificDisease = targetSpecificDisease;
                SpecificDisease = specificDisease;
                CureChance = cureChance;
            }
        }
}
