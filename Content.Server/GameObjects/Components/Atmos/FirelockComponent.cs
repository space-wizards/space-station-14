using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Doors;
using Content.Server.GameObjects.Components.Interactable;
using Content.Shared.GameObjects.Components.Doors;
using Content.Shared.GameObjects.Components.Interactable;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.Components.Atmos
{
    [RegisterComponent]
    public class FirelockComponent : ServerDoorComponent, IInteractUsing, IActivate, ICollideBehavior
    {
        public override string Name => "Firelock";

        public override void Initialize()
        {
            base.Initialize();
        }

        public void CollideWith(IEntity collidedWith)
        {
            // We do nothing.
        }

        public void Activate(ActivateEventArgs eventArgs)
        {
            // We do nothing.
        }

        protected override void Startup()
        {
            base.Startup();

            var airtightComponent = Owner.EnsureComponent<AirtightComponent>();
            var collidableComponent = Owner.GetComponent<ICollidableComponent>();

            Safety = false;
            airtightComponent.AirBlocked = false;
            collidableComponent.Hard = false;

            if (Occludes && Owner.TryGetComponent(out OccluderComponent occluder))
            {
                occluder.Enabled = false;
            }

            State = DoorState.Open;
            SetAppearance(DoorVisualState.Open);
        }

        public bool EmergencyPressureStop()
        {
            var closed = State == DoorState.Open && Close();

            if(closed)
                Owner.GetComponent<AirtightComponent>().AirBlocked = true;

            return closed;
        }

        public override void Deny()
        {
        }

        public override bool CanClose(IEntity user) => true;
        public override bool CanOpen(IEntity user) => true;

        public async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!eventArgs.Using.TryGetComponent<ToolComponent>(out var tool))
                return false;

            if (!await tool.UseTool(eventArgs.User, Owner, 3f, ToolQuality.Prying)) return false;

            if (State == DoorState.Closed)
                Open();
            else if (State == DoorState.Open)
                Close();

            return true;
        }
    }
}
