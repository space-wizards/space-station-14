using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;

namespace Content.Server.UserInterface
{
    [RegisterComponent]
    public sealed partial class ActivatableUIComponent : Component,
            ISerializationHooks
    {
        [ViewVariables]
        public Enum? Key { get; set; }

        [ViewVariables] public PlayerBoundUserInterface? UserInterface => (Key != null) ? Owner.GetUIOrNull(Key) : null;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("inHandsOnly")]
        public bool InHandsOnly { get; set; } = false;

        [DataField("singleUser")]
        public bool SingleUser { get; set; } = false;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("adminOnly")]
        public bool AdminOnly { get; set; } = false;

        [DataField("key", required: true)]
        private string _keyRaw = default!;

        [DataField("verbText")]
        public string VerbText = "ui-verb-toggle-open";

        /// <summary>
        ///     Whether you need a hand to operate this UI. The hand does not need to be free, you just need to have one.
        /// </summary>
        /// <remarks>
        ///     This should probably be true for most machines & computers, but there will still be UIs that represent a
        ///     more generic interaction / configuration that might not require hands.
        /// </remarks>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("requireHands")]
        public bool RequireHands = true;

        /// <summary>
        ///     Whether you can activate this ui with activateinhand or not
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("rightClickOnly")]
        public bool rightClickOnly = false;

        /// <summary>
        ///     Whether spectators (non-admin ghosts) should be allowed to view this UI.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("allowSpectator")]
        public bool AllowSpectator = true;

        /// <summary>
        ///     Whether the UI should close when the item is deselected due to a hand swap or drop
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("closeOnHandDeselect")]
        public bool CloseOnHandDeselect = true;

        /// <summary>
        ///     The client channel currently using the object, or null if there's none/not single user.
        ///     NOTE: DO NOT DIRECTLY SET, USE ActivatableUISystem.SetCurrentSingleUser
        /// </summary>
        [ViewVariables]
        public IPlayerSession? CurrentSingleUser;

        void ISerializationHooks.AfterDeserialization()
        {
            var reflectionManager = IoCManager.Resolve<IReflectionManager>();
            if (reflectionManager.TryParseEnumReference(_keyRaw, out var key))
                Key = key;
        }
    }
}

