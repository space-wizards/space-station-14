using Content.Server.Body.Components;
using Content.Shared.Inventory;

namespace Content.Server.Atmos.Components
{
    /// <summary>
    /// Used in internals as breath tool.
    /// </summary>
    [RegisterComponent]
    [ComponentProtoName("BreathMask")]
    public sealed class BreathToolComponent : Component
    {
        [Dependency] private readonly IEntityManager _entities = default!;

        /// <summary>
        /// Tool is functional only in allowed slots
        /// </summary>
        [DataField("allowedSlots")]
        public SlotFlags AllowedSlots = SlotFlags.MASK;
        public bool IsFunctional;
        public EntityUid ConnectedInternalsEntity;

        protected override void Shutdown()
        {
            base.Shutdown();
            DisconnectInternals();
        }

        public void DisconnectInternals()
        {
            var old = ConnectedInternalsEntity;
            ConnectedInternalsEntity = default;

            if (old != default && _entities.TryGetComponent<InternalsComponent?>(old, out var internalsComponent))
            {
                internalsComponent.DisconnectBreathTool();
            }

            IsFunctional = false;
        }
    }
}
