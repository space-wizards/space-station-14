using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Doors;
using Robust.Shared.GameObjects.Systems;

namespace Content.Shared.GameObjects.EntitySystems
{
    /// <summary>
    /// Used on both server and client side as part of ServerDoorSystem and ClientDoorSystem, to periodically call OnUpdate on relevant door components.
    /// </summary>
    public abstract class SharedDoorSystem : EntitySystem
    {
        /// <summary>
        /// List of doors that need to be periodically updated by this system.
        /// </summary>
        protected readonly List<SharedDoorComponent> ActiveDoors = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DoorStateMessage>(HandleDoorState);
        }

        /// <summary>
        /// Used to register doors to / from ActiveDoors, the list of doors that need to be periodically updated by this system.
        /// </summary>
        /// <param name="message">A message corresponding to the component under consideration, raised when its state changes.</param>
        protected abstract void HandleDoorState(DoorStateMessage message);

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            for (var i = ActiveDoors.Count - 1; i >= 0; i--)
            {
                var comp = ActiveDoors[i];
                if (comp.Deleted)
                    ActiveDoors.RemoveAt(i);

                comp.OnUpdate(frameTime);
            }
        }
    }
}
