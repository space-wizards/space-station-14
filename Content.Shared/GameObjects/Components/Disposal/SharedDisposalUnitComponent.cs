using System;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Disposal
{
    public abstract class SharedDisposalUnitComponent : Component
    {
        public override string Name => "DisposalUnit";

        [Serializable, NetSerializable]
        public enum Visuals
        {
            VisualState,
        }

        [Serializable, NetSerializable]
        public enum VisualState
        {
            UnAnchored,
            Anchored,
            Flushing
        }

        [Serializable, NetSerializable]
        public enum UiButton
        {
            Eject,
            Engage,
            Power
        }

        [Serializable, NetSerializable]
        public class DisposalUnitBoundUserInterfaceState : BoundUserInterfaceState
        {
            public readonly string UnitName;
            public readonly float Pressure;
            public readonly bool Powered;

            public DisposalUnitBoundUserInterfaceState(string unitName, float pressure, bool powered)
            {
                UnitName = unitName;
                Pressure = pressure;
                Powered = powered;
            }
        }

        /// <summary>
        ///     Message data sent from client to server when a disposal unit ui button is pressed.
        /// </summary>
        [Serializable, NetSerializable]
        public class UiButtonPressedMessage : BoundUserInterfaceMessage
        {
            public readonly UiButton Button;

            public UiButtonPressedMessage(UiButton button)
            {
                Button = button;
            }
        }

        [Serializable, NetSerializable]
        public enum DisposalUnitUiKey
        {
            Key
        }
    }
}
