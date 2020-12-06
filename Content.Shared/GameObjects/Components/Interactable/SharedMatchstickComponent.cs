using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Interactable
{
    public class SharedMatchstickComponent: Component
    {
        public override string Name => "Matchstick";
    }
    [Serializable, NetSerializable]
    public enum MatchstickVisual
    {
        Igniting,
    }

    [Serializable, NetSerializable]
    public enum MatchstickState
    {
        Unlit,
        Lit,
        Burnt,
    }
}
