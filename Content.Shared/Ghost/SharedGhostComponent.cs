using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Ghost
{
    [NetworkedComponent]
    [AutoGenerateComponentState]
    public abstract partial class SharedGhostComponent : Component
    {
        // TODO: instead of this funny stuff just give it access and update in system dirtying when needed
        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanGhostInteract
        {
            get => _canGhostInteract;
            set
            {
                if (_canGhostInteract == value) return;
                _canGhostInteract = value;
                Dirty();
            }
        }

        [DataField("canInteract"), AutoNetworkedField]
        private bool _canGhostInteract;

        /// <summary>
        ///     Changed by <see cref="SharedGhostSystem.SetCanReturnToBody"/>
        /// </summary>
        // TODO MIRROR change this to use friend classes when thats merged
        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanReturnToBody
        {
            get => _canReturnToBody;
            set
            {
                if (_canReturnToBody == value) return;
                _canReturnToBody = value;
                Dirty();
            }
        }

        [DataField("canReturnToBody"), AutoNetworkedField]
        private bool _canReturnToBody;
    }
}


