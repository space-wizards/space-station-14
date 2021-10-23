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
    [RegisterComponent]
    public class GuardianHostComponent : Component
    {
        public override string Name => "GuardianHost";

        [ViewVariables] public ContainerSlot GuardianContainer = default!; //The container which hosts the guardian

        public bool IsGuardianHosted => GuardianContainer.ContainedEntity != null; //Checks if a guardian is inside the container

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
