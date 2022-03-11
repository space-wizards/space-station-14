using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Shared.Disease;
using Content.Server.Disease;
using Content.Server.Disease.Components;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;


namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems
{
    public sealed class DiseaseArtifactSystem : EntitySystem
    {
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        public override void Initialize()
        {
            base.Initialize();
        SubscribeLocalEvent<DiseaseArtifactComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<DiseaseArtifactComponent, ArtifactActivatedEvent>(OnActivate);
        }

        private void OnMapInit(EntityUid uid, DiseaseArtifactComponent component, MapInitEvent args)
        {
            if (component.SpawnDisease == string.Empty && component.ArtifactDiseases.Count != 0)
            {
                var diseaseName = _random.Pick(component.ArtifactDiseases);

                if (diseaseName != null)
                    component.SpawnDisease = diseaseName;
            }

            if (_prototypeManager.TryIndex(component.SpawnDisease, out DiseasePrototype? disease) && disease != null)
                component.ResolveDisease = disease;
        }

        private void OnActivate(EntityUid uid, DiseaseArtifactComponent component, ArtifactActivatedEvent args)
        {
            var xform = Transform(uid);
            foreach (var entity in _lookup.GetEntitiesInRange(xform.MapID, xform.WorldPosition, 1.5f))
            {
                if (TryComp<DiseaseCarrierComponent>(entity, out var carrier))
                    EntitySystem.Get<DiseaseSystem>().TryInfect(carrier, component.ResolveDisease);
            }

        }
    }
}

