#nullable enable
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;

namespace Content.Shared.GameObjects.Components.Disposal.DisposalUnit
{
    public class SharedDisposalUnitComponent : Component
    {

        public override string Name => "DisposalUnit";

        [ViewVariables]
        public bool Anchored => !Owner.TryGetComponent(out IPhysicsComponent? physics) || physics.Anchored;

        [Serializable, NetSerializable]
        public enum Visuals
        {
            VisualState,
            Handle,
            Light
        }

        [Serializable, NetSerializable]
        public enum VisualState
        {
            UnAnchored,
            Anchored,
            Flushing,
            Charging
        }

        [Serializable, NetSerializable]
        public enum HandleState
        {
            Normal,
            Engaged
        }

        [Serializable, NetSerializable]
        public enum LightState
        {
            Off,
            Charging,
            Full,
            Ready
        }

        [Serializable, NetSerializable]
        public enum PressureState
        {
            Ready,
            Pressurizing
        }

        [Serializable, NetSerializable]
        public enum DisposalUnitUiKey
        {
            Key
        }
    }
}
