using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Cuffs.Components
{
    [NetworkedComponent()]
    public abstract class SharedCuffableComponent : Component
    {
        [ViewVariables]
        public bool CanStillInteract { get; set; } = true;

        [Serializable, NetSerializable]
        protected sealed class CuffableComponentState : ComponentState
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
    }
}
