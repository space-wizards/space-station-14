using System;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.Components
{
    [NetworkedComponent()]
    public class SharedHealthAnalyzerComponent : Component
    {
        public override string Name => "HealthAnalyzer";

        [Serializable, NetSerializable]
        public enum HealthAnalyzerUiKey
        {
            Key,
        }

        [Serializable, NetSerializable]
        public class HealthAnalyzerBoundUserInterfaceState : BoundUserInterfaceState
        {
            public float Health;
            //public OrganEntry[]? Organs;
            public string? Error;
            public HealthAnalyzerBoundUserInterfaceState(float health, string? error = null)
            {
                Health = health;
                Error = error;
            }
        }

        [Serializable, NetSerializable]
        public class HealthAnalyzerRefreshMessage : BoundUserInterfaceMessage
        {
            public HealthAnalyzerRefreshMessage() {}
        }
    }

    [NetSerializable]
    [Serializable]
    public enum HealthAnalyzerVisuals
    {
        VisualState,
    }

    [NetSerializable]
    [Serializable]
    public enum HealthAnalyzerVisualState
    {
        Off,
        Working,
    }
}
