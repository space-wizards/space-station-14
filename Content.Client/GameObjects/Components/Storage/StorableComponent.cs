#nullable enable
using Content.Shared.GameObjects.Components.Storage;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Storage
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

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (!(curState is StorableComponentState state))
            {
                return;
            }

            _size = state.Size;
        }
    }
}
