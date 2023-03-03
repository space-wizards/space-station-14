using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Cuffs.Components
{
    [RegisterComponent, NetworkedComponent]
    public sealed class CuffableComponent : Component
    {
        [ViewVariables]
        public string? CurrentRSI;

        /// <summary>
        /// How many of this entity's hands are currently cuffed.
        /// </summary>
        [ViewVariables]
        public int CuffedHandCount => Container.ContainedEntities.Count * 2;

        public EntityUid LastAddedCuffs => Container.ContainedEntities[^1];

        /// <summary>
        ///     Container of various handcuffs currently applied to the entity.
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        public Container Container { get; set; } = default!;

        [ViewVariables]
        public bool CanStillInteract { get; set; } = true;

        [DataField("uncuffing")]
        public bool Uncuffing;

    }

    [Serializable, NetSerializable]
    public sealed class CuffableComponentState : ComponentState
    {
        public bool CanStillInteract { get; }
        public int NumHandsCuffed { get; }
        public string? RSI { get; }
        public string IconState { get; }
        public Color Color { get; }

        public CuffableComponentState(int numHandsCuffed, bool canStillInteract, string? rsiPath, string iconState, Color color)
        {
            NumHandsCuffed = numHandsCuffed;
            CanStillInteract = canStillInteract;
            RSI = rsiPath;
            IconState = iconState;
            Color = color;
        }
    }

    [ByRefEvent]
    public readonly record struct CuffedStateChangeEvent;
}
