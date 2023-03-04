using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Cuffs.Components
{
    [RegisterComponent, NetworkedComponent]
    public sealed class CuffableComponent : Component
    {
        /// <summary>
        /// The current RSI for the handcuff layer
        /// </summary>
        [ViewVariables]
        public string? CurrentRSI;

        /// <summary>
        /// How many of this entity's hands are currently cuffed.
        /// </summary>
        [ViewVariables]
        public int CuffedHandCount => Container.ContainedEntities.Count * 2;

        /// <summary>
        /// The last pair of cuffs that was added to this entity.
        /// </summary>
        [ViewVariables]
        public EntityUid LastAddedCuffs => Container.ContainedEntities[^1];

        /// <summary>
        ///     Container of various handcuffs currently applied to the entity.
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        public Container Container = default!;

        /// <summary>
        /// Whether or not the entity can still interact (is not cuffed)
        /// </summary>
        [DataField("canStillInteract")]
        public bool CanStillInteract = true;

        /// <summary>
        /// Whether or not the entity is currently in the process of being uncuffed.
        /// </summary>
        [DataField("uncuffing")]
        public bool Uncuffing;
    }

    [Serializable, NetSerializable]
    public sealed class CuffableComponentState : ComponentState
    {
        public bool CanStillInteract;
        public bool Uncuffing;
        public int NumHandsCuffed;
        public string? RSI;
        public string IconState;
        public Color Color;

        public CuffableComponentState(int numHandsCuffed, bool canStillInteract,  bool uncuffing, string? rsiPath, string iconState, Color color)
        {
            NumHandsCuffed = numHandsCuffed;
            CanStillInteract = canStillInteract;
            Uncuffing = uncuffing;
            RSI = rsiPath;
            IconState = iconState;
            Color = color;
        }
    }

    [ByRefEvent]
    public readonly record struct CuffedStateChangeEvent;
}
