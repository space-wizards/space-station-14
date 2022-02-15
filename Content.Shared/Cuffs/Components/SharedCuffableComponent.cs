using System;
using Content.Shared.ActionBlocker;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Cuffs.Components
{
    [NetworkedComponent()]
    public abstract class SharedCuffableComponent : Component
    {
        [ViewVariables]
        public bool CanStillInteract
        {
            get => _canStillInteract;
            set
            {
                if (_canStillInteract == value) return;
                EntitySystem.Get<ActionBlockerSystem>().RefreshCanMove(Owner);
            }
        }

        private bool _canStillInteract = true;

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
