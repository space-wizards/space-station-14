using System;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
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
        public class ConsoleUnlockTechnology : BoundUserInterfaceMessage
        {
            public string Id;
            public ConsoleUnlockTechnology(string id)
            {
                Id = id;
            }
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
