using Content.Shared.GameObjects.Components.Storage;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Items.Storage
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedStorableComponent))]
    public class StorableComponent : SharedStorableComponent
    {
        private int _size;

        public override int Size
        {
            get => _size;
            set
            {
                if (_size == value)
                {
                    return;
                }

                _size = value;

                Dirty();
            }
        }

        public override ComponentState GetComponentState()
        {
            return new StorableComponentState(_size);
        }
    }

    /// <summary>
    /// Enum for the storage capacity of various containers
    /// </summary>
    public enum ReferenceSizes
    {
        Wallet = 4,
        Pocket = 12,
        Box = 24,
        Belt = 30,
        Toolbox = 60,
        Backpack = 100,
        NoStoring = 9999
    }
}
