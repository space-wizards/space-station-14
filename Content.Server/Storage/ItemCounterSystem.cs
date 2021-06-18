using Content.Server.Storage.Components;
using Content.Shared.Storage;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Storage
{
    [UsedImplicitly]
    public class ItemCounterSystem : SharedItemCounterSystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SharedItemCounterComponent, ComponentInit>(OnComponentInit);
        }

        private void OnComponentInit(EntityUid uid, SharedItemCounterComponent component, ComponentInit args)
        {
            if (!ComponentManager.TryGetComponent(uid, out SharedAppearanceComponent? appearance)
                || !component.Owner.TryGetComponent(out ServerStorageComponent? _))
                return;

            appearance.SetData(StorageMapVisuals.AllLayers, component.SpriteLayers);
        }
    }
}
