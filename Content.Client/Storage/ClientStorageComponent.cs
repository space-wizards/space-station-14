using Content.Client.Animations;
using Content.Shared.DragDrop;
using Content.Shared.Storage;

namespace Content.Client.Storage
{
    /// <summary>
    /// Client version of item storage containers, contains a UI which displays stored entities and their size
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(SharedStorageComponent))]
    public sealed partial class ClientStorageComponent : SharedStorageComponent
    {
        private List<EntityUid> _storedEntities = new();
        public override IReadOnlyList<EntityUid> StoredEntities => _storedEntities;

        public override bool Remove(EntityUid entity)
        {
            return false;
        }
    }
}
