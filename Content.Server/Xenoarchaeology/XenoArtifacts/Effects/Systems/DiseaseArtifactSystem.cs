using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Shared.Disease;
using Content.Server.Disease;
using Content.Server.Disease.Components;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;
using Content.Shared.Interaction;
using Robust.Shared.Map;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems
{
    /// <summary>
    /// Handles disease-producing artifacts
    /// </summary>
    public sealed class DiseaseArtifactSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly DiseaseSystem _disease = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;

        // TODO: YAML Serializer won't catch this.
        [ViewVariables(VVAccess.ReadWrite)]
        public readonly IReadOnlyList<string> ArtifactDiseases = new[]
        {
            "VanAusdallsRobovirus",
            "OwOnavirus",
            "BleedersBite",
            "Ultragigacancer",
            "AMIV"
        };

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
            if (component.SpawnDisease != null || ArtifactDiseases.Count == 0) return;
            var diseaseName = _random.Pick(ArtifactDiseases);

            if (!_prototypeManager.TryIndex<DiseasePrototype>(diseaseName, out var disease))
            {
                Logger.ErrorS("disease", $"Invalid disease {diseaseName} selected from random diseases.");
                return;
            }

            component.SpawnDisease = disease;
        }

        /// <summary>
        /// When activated, blasts everyone in LOS within n tiles
        /// with a high-probability disease infection attempt
        /// </summary>
        private void OnActivate(EntityUid uid, DiseaseArtifactComponent component, ArtifactActivatedEvent args)
        {
            if (component.SpawnDisease == null) return;

            var xform = Transform(uid);
            var carrierQuery = GetEntityQuery<DiseaseCarrierComponent>();

            foreach (var entity in _lookup.GetEntitiesInRange(xform.Coordinates, component.Range))
            {
                if (!carrierQuery.TryGetComponent(entity, out var carrier)) continue;

                if (!_interactionSystem.InRangeUnobstructed(uid, entity, component.Range))
                    continue;

                _disease.TryInfect(carrier, component.SpawnDisease, forced: true);
            }
        }
    }
}

