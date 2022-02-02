using System;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Research.Components
{
    [NetworkedComponent()]
    public class SharedResearchConsoleComponent : Component
    {

        [NetSerializable, Serializable]
        public enum ResearchConsoleUiKey
        {
            Key,
        }

        [Serializable, NetSerializable]
        public class ConsoleUnlockTechnologyMessage : BoundUserInterfaceMessage
        {
            public string Id;

            public ConsoleUnlockTechnologyMessage(string id)
            {
                Id = id;
            }
        }

        [Serializable, NetSerializable]
        public class ConsoleServerSyncMessage : BoundUserInterfaceMessage
        {
            public ConsoleServerSyncMessage()
            {}
        }

        [Serializable, NetSerializable]
        public class ConsoleServerSelectionMessage : BoundUserInterfaceMessage
        {
            public ConsoleServerSelectionMessage()
            {}
        }

        [Serializable, NetSerializable]
        public sealed class ResearchConsoleBoundInterfaceState : BoundUserInterfaceState
        {
            public int Points;
            public int PointsPerSecond;
            public ResearchConsoleBoundInterfaceState(int points, int pointsPerSecond)
            {
                Points = points;
                PointsPerSecond = pointsPerSecond;
            }
        }
    }
}
