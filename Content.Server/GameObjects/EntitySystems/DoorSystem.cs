using Content.Server.GameObjects.Components.Doors;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    class DoorSystem : EntitySystem
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
        }

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            foreach (var comp in ComponentManager.EntityQuery<ServerDoorComponent>())
            {
                comp.OnUpdate(frameTime);
            }
        }
    }
}
