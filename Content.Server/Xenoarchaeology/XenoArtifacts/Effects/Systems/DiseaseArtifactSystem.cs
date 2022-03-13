using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Shared.Disease;
using Content.Server.Disease;
using Content.Server.Disease.Components;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;
using Content.Shared.Interaction;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems
{
    /// <summary>
    /// Handles disease-producing artifacts
    /// </summary>
    public sealed class DiseaseArtifactSystem : EntitySystem
    {
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DiseaseArtifactComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<DiseaseArtifactComponent, ArtifactActivatedEvent>(OnActivate);
        }

        /// <summary>
        /// Makes sure this artifact is assigned a disease
        /// </summary>
        private void OnMapInit(EntityUid uid, DiseaseArtifactComponent component, MapInitEvent args)
        {
            if (component.SpawnDisease == string.Empty && component.ArtifactDiseases.Count != 0)
            {
                var diseaseName = _random.Pick(component.ArtifactDiseases);

                component.SpawnDisease = diseaseName;
            }

            if (_prototypeManager.TryIndex(component.SpawnDisease, out DiseasePrototype? disease) && disease != null)
                component.ResolveDisease = disease;
        }

        /// <summary>
        /// When activated, blasts everyone in LOS within 3 tiles
        /// with a high-probability disease infection attempt
        /// </summary>
        private void OnActivate(EntityUid uid, DiseaseArtifactComponent component, ArtifactActivatedEvent args)
        {
            var xform = Transform(uid);
            foreach (var entity in _lookup.GetEntitiesInRange(xform.MapID, xform.WorldPosition, 3f))
            {
                if (!_interactionSystem.InRangeUnobstructed(uid, entity, 3f))
                    continue;

                if (TryComp<DiseaseCarrierComponent>(entity, out var carrier))
                    EntitySystem.Get<DiseaseSystem>().TryInfect(carrier, component.ResolveDisease);
            }
        }
    }
}

