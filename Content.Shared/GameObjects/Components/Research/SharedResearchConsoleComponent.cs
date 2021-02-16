using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Research
{
    public class SharedResearchConsoleComponent : Component
    {
        public override string Name => "ResearchConsole";
        public override uint? NetID => ContentNetIDs.RESEARCH_CONSOLE;

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
