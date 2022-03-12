using Content.Server.Disease.Components;
using Content.Shared.Disease;
using Robust.Shared.Prototypes;
using Content.Shared.Interaction;

namespace Content.Server.Disease
{
    public sealed class DiseaseGiverSystem : DiseaseSystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DiseaseGiverComponent, AfterInteractEvent>(OnAfterInteract);
        }

        private void OnAfterInteract(EntityUid uid, DiseaseGiverComponent component, AfterInteractEvent args)
        {
            if (!_prototypeManager.TryIndex(component.Disease, out DiseasePrototype? compDisease))
                return;

            if (!TryComp<DiseaseCarrierComponent>(args.Target, out var targetDiseases) || targetDiseases == null)
                return;
            TryAddDisease(targetDiseases, compDisease);
        }
    }
}
