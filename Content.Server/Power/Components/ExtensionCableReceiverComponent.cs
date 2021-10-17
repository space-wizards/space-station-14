using Content.Server.Power.EntitySystems;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Power.Components
{
    [RegisterComponent]
    [Friend(typeof(ExtensionCableSystem))]
    public class ExtensionCableReceiverComponent : Component
    {
        public override string Name => "ExtensionCableReceiver";

        [ViewVariables]
        public ExtensionCableProviderComponent? Provider { get; set; }

        [ViewVariables]
        public bool Connectable = false;

        /// <summary>
        ///     The max distance from a <see cref="ExtensionCableProviderComponent"/> that this can receive power from.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("receptionRange")]
        public int ReceptionRange { get; set; } = 3;
    }
}
