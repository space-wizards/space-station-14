using System;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Doors;
using Content.Server.GameObjects.Components.Interactable;
using Content.Shared.GameObjects.Components.Doors;
using Content.Shared.GameObjects.Components.Interactable;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Atmos
{
    [RegisterComponent]
    [ComponentReference(typeof(ServerDoorComponent))]
    public class FirelockComponent : ServerDoorComponent, IInteractUsing, ICollideBehavior
    {
        public override string Name => "Firelock";

        protected override TimeSpan CloseTimeOne => TimeSpan.FromSeconds(0.1f);
        protected override TimeSpan CloseTimeTwo => TimeSpan.FromSeconds(0.6f);
        protected override TimeSpan OpenTimeOne => TimeSpan.FromSeconds(0.1f);
        protected override TimeSpan OpenTimeTwo => TimeSpan.FromSeconds(0.6f);

        public void CollideWith(IEntity collidedWith)
        {
            // We do nothing.
        }

        protected override void Startup()
        {
            base.Startup();

            if (Owner.TryGetComponent(out AirtightComponent airtightComponent))
            {
                airtightComponent.AirBlocked = false;
            }

            if (Owner.TryGetComponent(out IPhysicsComponent physics))
            {
                physics.CanCollide = false;
            }

            AutoClose = false;
            Safety = false;

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

        public override bool CanOpen()
        {
            return !IsHoldingFire() && !IsHoldingPressure() && base.CanOpen();
        }

        public override bool CanClose(IEntity user) => true;
        public override bool CanOpen(IEntity user) => CanOpen();

        public override async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (await base.InteractUsing(eventArgs))
                return false;

            if (!eventArgs.Using.TryGetComponent<ToolComponent>(out var tool))
                return false;

            if (tool.HasQuality(ToolQuality.Prying) && !IsWeldedShut)
            {
                var holdingPressure = IsHoldingPressure();
                var holdingFire = IsHoldingFire();

                if (State == DoorState.Closed)
                {
                    if (holdingPressure)
                        Owner.PopupMessage(eventArgs.User, "A gush of air blows in your face... Maybe you should reconsider.");
                }

                if (IsWeldedShut || !await tool.UseTool(eventArgs.User, Owner, holdingPressure || holdingFire ? 1.5f : 0.25f, ToolQuality.Prying)) return false;
                if (State == DoorState.Closed)
                {
                    Open();
                }
                else if (State == DoorState.Open)
                {
                    Close();
                }


                return true;
            }


            return false;
        }
    }
}
