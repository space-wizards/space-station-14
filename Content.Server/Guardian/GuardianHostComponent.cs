using Content.Server.Construction.Components;
using Content.Server.Power.Components;
using Content.Shared.Computer;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Guardian
{
    /// <summary>
    /// Given to guardian users upon establishing a guardian link with the entity
    /// </summary>
    [RegisterComponent]
    public class GuardianHostComponent : Component
    {
        public override string Name => "GuardianHost";

        /// <summary>
        /// Guardian hosted within the component
        /// </summary>
        public EntityUid Hostedguardian;

        /// <summary>
        /// Container which holds the guardian
        /// </summary>
        [ViewVariables] public ContainerSlot GuardianContainer = default!;

        protected override void Initialize()
        {
            base.Initialize();
            GuardianContainer = ContainerHelpers.EnsureContainer<ContainerSlot>(Owner, "GuardianContainer");
        }

        public void InsertBody(IEntity guardian)
        {
            GuardianContainer.Insert(guardian);
        }

        public void EjectBody()
        {
            var containedEntity = GuardianContainer.ContainedEntity;
            if (containedEntity == null) return;
            GuardianContainer.Remove(containedEntity);
        }
    }
}
