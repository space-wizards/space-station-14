using Content.Shared.Disease;
using Content.Shared.Interaction;
using Content.Server.Popups;
using Robust.Shared.Player;

namespace Content.Server.Disease
{
    public sealed class DiseaseSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
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
                                    carrierComp.Diseases.Remove(disease);
                                    _popupSystem.PopupEntity(Loc.GetString("disease-cured"), carrierComp.Owner, Filter.Pvs(carrierComp.Owner));
                                    return; // Get the hell out before we trigger enumeration errors, sorry you can only cure like 30 diseases a second
                                }
                            foreach (var effect in disease.Effects)
                                effect.Effect(args);
                        }
                    }
               if (carrierComp.Diseases.Count == 0)
                  RemComp<DiseasedComponent>(diseasedComp.Owner);
                }
            }
        }

        private void OnAfterInteract(EntityUid uid, DiseaseInteractSourceComponent component, AfterInteractEvent args)
        {
            if (!args.CanReach || args.Target == null)
                return;

            if (!TryComp<DiseaseCarrierComponent>(args.Target, out var targetDiseases) || targetDiseases == null)
                return;
            if (targetDiseases.Diseases.Count > 0)
            {
                foreach (var disease in targetDiseases.Diseases)
                {
                    if (disease.Name == component.Disease.Name)
                        return;
                }
            }
            targetDiseases.Diseases.Add(component.Disease);
            EnsureComp<DiseasedComponent>(args.Target.Value);
        }
    }
}
