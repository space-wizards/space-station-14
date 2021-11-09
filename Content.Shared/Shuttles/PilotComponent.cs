using System;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Shuttles
{
    /// <summary>
    /// Stores what shuttle this entity is currently piloting.
    /// </summary>
    [RegisterComponent]
    [NetworkedComponent()]
    public sealed class PilotComponent : Component
    {
        public override string Name => "Pilot";
        [ViewVariables] public SharedShuttleConsoleComponent? Console { get; set; }

        /// <summary>
        /// Position from where we started piloting to determine if we should break.
        /// </summary>
        [ViewVariables]
        public EntityCoordinates? PilotPosition { get; set; }

        public const float BreakDistance = 0.5f;

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);
            if (curState is not PilotComponentState state) return;

            if (state.Console == null)
            {
                Console = null;
                return;
            }

            if (!Owner.EntityManager.TryGetEntity(state.Console.Value, out var consoleEnt) ||
                !consoleEnt.TryGetComponent(out SharedShuttleConsoleComponent? shuttleConsoleComponent))
            {
                Logger.Warning($"Unable to set Helmsman console to {state.Console.Value}");
                return;
            }

            Console = shuttleConsoleComponent;
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new PilotComponentState(Console?.Owner.Uid);
        }

        [Serializable, NetSerializable]
        private sealed class PilotComponentState : ComponentState
        {
            public EntityUid? Console { get; }

            public PilotComponentState(EntityUid? uid)
            {
                Console = uid;
            }
        }
    }
}
