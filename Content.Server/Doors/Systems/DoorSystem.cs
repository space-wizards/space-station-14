using Content.Server.Doors.Components;
using Content.Shared.Doors;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics.Dynamics;

namespace Content.Server.Doors
{
    /// <summary>
    /// Used on the server side to manage global access level overrides.
    /// </summary>
    internal sealed class DoorSystem : SharedDoorSystem
    {
        /// <summary>
        ///     Determines the base access behavior of all doors on the station.
        /// </summary>
        public AccessTypes AccessType { get; set; }

        /// <summary>
        /// How door access should be handled.
        /// </summary>
        public enum AccessTypes
        {
            /// <summary> ID based door access. </summary>
            Id,
            /// <summary>
            /// Allows everyone to open doors, except external which airlocks are still handled with ID's
            /// </summary>
            AllowAllIdExternal,
            /// <summary>
            /// Allows everyone to open doors, except external airlocks which are never allowed, even if the user has
            /// ID access.
            /// </summary>
            AllowAllNoExternal,
            /// <summary> Allows everyone to open all doors. </summary>
            AllowAll
        }

        public override void Initialize()
        {
            base.Initialize();

            AccessType = AccessTypes.Id;
            SubscribeLocalEvent<ServerDoorComponent, StartCollideEvent>(HandleCollide);
        }

        private void HandleCollide(EntityUid uid, ServerDoorComponent component, StartCollideEvent args)
        {
            if (!EntityManager.HasComponent<DoorBumpOpenerComponent>(args.OtherFixture.Body.Owner))
            {
                return;
            }

            if (component.State != SharedDoorComponent.DoorState.Closed)
            {
                return;
            }

            if (!component.BumpOpen)
            {
                return;
            }

            // Disabled because it makes it suck hard to walk through double doors.

            component.TryOpen(args.OtherFixture.Body.Owner);
        }
    }
}
