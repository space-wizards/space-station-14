using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Shared.UserInterface
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class ActivatableUIComponent : Component
    {
        [DataField(required: true, customTypeSerializer: typeof(EnumSerializer))]
        public Enum? Key;

        /// <summary>
        /// Whether the item must be held in one of the user's hands to work.
        /// This is ignored unless <see cref="RequiresComplex"/> is true.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public bool InHandsOnly;

        [DataField]
        public bool SingleUser;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public bool AdminOnly;

        [DataField]
        public LocId VerbText = "ui-verb-toggle-open";

        /// <summary>
        ///     Whether you need to be able to do complex interactions to operate this UI.
        /// </summary>
        /// <remarks>
        ///     This should probably be true for most machines & computers, but there will still be UIs that represent a
        ///     more generic interaction / configuration that might not require complex.
        /// </remarks>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public bool RequiresComplex = true;

        /// <summary>
        ///     Entities that are required to open this UI.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public EntityWhitelist? RequiredItems;

        /// <summary>
        ///     If true, then this UI can only be opened via verbs. I.e., normal interactions/activations will not open
        ///     the UI.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public bool VerbOnly;

        /// <summary>
        ///     Whether spectators (non-admin ghosts) should be allowed to view this UI.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public bool BlockSpectators;

        /// <summary>
        ///     Whether the item must be in the user's currently selected/active hand.
        ///     This is ignored unless <see cref="InHandsOnly"/> is true.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public bool RequireActiveHand = true;

        /// <summary>
        ///     The client channel currently using the object, or null if there's none/not single user.
        ///     NOTE: DO NOT DIRECTLY SET, USE ActivatableUISystem.SetCurrentSingleUser
        /// </summary>
        [DataField, AutoNetworkedField]
        public EntityUid? CurrentSingleUser;
    }
}
