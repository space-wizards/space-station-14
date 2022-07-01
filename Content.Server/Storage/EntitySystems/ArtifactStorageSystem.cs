using Content.Server.Storage.Components;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.Containers;

namespace Content.Server.Storage.EntitySystems
{
    public sealed class ArtifactStorageSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ArtifactStorageComponent, StorageBeforeCloseEvent>(OnBeforeClose);
        }

        private void OnBeforeClose(EntityUid uid, ArtifactStorageComponent component, StorageBeforeCloseEvent args)
        {
            //FIX LATER
            //COMPLICATED
            // :-(
        }
    }
}
