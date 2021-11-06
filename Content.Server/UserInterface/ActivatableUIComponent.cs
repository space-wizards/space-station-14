using System;
using Content.Shared.Instruments;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Reflection;
using Robust.Shared.GameObjects;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Network;
using Robust.Shared.IoC;
using Robust.Shared.Utility;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.UserInterface
{
    [RegisterComponent]
    public class ActivatableUIComponent : Component,
            ISerializationHooks
    {
        public override string Name => "ActivatableUI";

        private ActivatableUISystem _activatableUISystem = default!;

        [ViewVariables]
        public Enum? Key { get; set; }

        [ViewVariables] public BoundUserInterface? UserInterface => (Key != null) ? Owner.GetUIOrNull(Key) : null;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("inHandsOnly")]
        public bool InHandsOnly { get; set; } = false;

        [ViewVariables]
        [DataField("singleUser")]
        public bool SingleUser { get; set; } = false;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("adminOnly")]
        public bool AdminOnly { get; set; } = false;

        [DataField("key", readOnly: true, required: true)]
        private string _keyRaw = default!;

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

        public void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
        {
            // Has to be here for delegate to bind correctly.
            // This *CANNOT* be elevated to system level because otherwise:
            // pick one:
            // + as method: delegate doesn't get the context it needs
            //              (and will break if multiple show up)
            // + as lambda: it can't be properly removed
            // This probably ought to be BUI's responsibility anyway
            // A disconnect should count as closing the UI, rest follows automatically
            if (Deleted || e.Session != CurrentSingleUser || e.NewStatus != SessionStatus.Disconnected) return;
            _activatableUISystem.SetCurrentSingleUser(OwnerUid, null, this);
        }

        protected override void Initialize()
        {
            base.Initialize();

            // Note the implicit exception thrown if the key is messed up
            // This is on purpose for fail-fast
            var ui = UserInterface;
            if (ui != null)
                ui.OnClosed += UserInterfaceOnClosed;

            _activatableUISystem = EntitySystem.Get<ActivatableUISystem>();
        }

        private void UserInterfaceOnClosed(IPlayerSession player)
        {
            if (player != CurrentSingleUser) return;
            _activatableUISystem.SetCurrentSingleUser(OwnerUid, null, this);
        }
    }
}

