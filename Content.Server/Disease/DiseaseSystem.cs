using Content.Shared.Disease;
using Content.Shared.Interaction;

namespace Content.Server.Disease
{
    public sealed class DiseaseSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DiseaseInteractSourceComponent, AfterInteractEvent>(OnAfterInteract);
        }
        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var diseasedComp in EntityQuery<DiseasedComponent>(false))
            {
                foreach(var disease in diseasedComp.Diseases)
                {
                    var args = new DiseaseEffectArgs(diseasedComp.Owner, disease);
                    disease.Accumulator += frameTime;
                    if (disease.Accumulator >= frameTime)
                    {
                        disease.Accumulator -= frameTime;
                        foreach (var cure in disease.Cures)
                            if (cure.Cure(args))
                            {
                                diseasedComp.Diseases.Remove(disease);
                            }
                        foreach (var effect in disease.Effects)
                            effect.Effect(args);
                    }
                }
                if (diseasedComp.Diseases.Count == 0)
                    RemComp<DiseasedComponent>(diseasedComp.Owner);
            }
        }

        private void OnAfterInteract(EntityUid uid, DiseaseInteractSourceComponent component, AfterInteractEvent args)
        {
            if (!args.CanReach || args.Target == null)
                return;

            var targetDiseaseComp = EnsureComp<DiseasedComponent>(args.Target.Value);

            if (targetDiseaseComp.Diseases.Count == 0)
            {
                targetDiseaseComp.Diseases = component.Diseases;
                return;
            }

            foreach (var disease in component.Diseases)
            {
                bool merge = true;
                foreach (var targetDisease in targetDiseaseComp.Diseases)
                {
                    if (targetDisease.Name == disease.Name)
                    {
                        merge = false;
                        continue;
                    }
                }
                if (merge)
                {
                    targetDiseaseComp.Diseases.Add(disease);
                }
            }
        }
    }
}
