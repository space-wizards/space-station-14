using System;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Tools.Components
{
    [NetworkedComponent]
    public abstract class SharedMultipleToolComponent : Component
    {
    }

    [NetSerializable, Serializable]
    public sealed class MultipleToolComponentState : ComponentState
    {
        public string QualityName { get; }

        public MultipleToolComponentState(string qualityName)
        {
            QualityName = qualityName;
        }
    }
}
