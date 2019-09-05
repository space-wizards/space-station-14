using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components
{
    public class SharedWiresComponent : Component
    {
        public override string Name => "Wires";

        [Serializable, NetSerializable]
        public enum WiresVisuals
        {
            MaintenancePanelState
        }

        [Serializable, NetSerializable]
        public enum WiresUiKey
        {
            Key,
        }

        [Serializable, NetSerializable]
        public enum WiresAction
        {
            Mend,
            Cut,
            Pulse,
        }

        [Serializable, NetSerializable]
        public class WiresBoundUserInterfaceState : BoundUserInterfaceState
        {
            public readonly List<ClientWire> WiresList;
            public readonly List<string> Statuses;

            public WiresBoundUserInterfaceState(List<ClientWire> wiresList, List<string> statuses)
            {
                WiresList = wiresList;
                Statuses = statuses;
            }
        }

        [Serializable, NetSerializable]
        public class ClientWire
        {
            public Guid Guid;
            public Color Color;
            public bool IsCut;

            public ClientWire(Guid guid, Color color, bool isCut)
            {
                Guid = guid;
                Color = color;
                IsCut = isCut;
            }
        }

        [Serializable, NetSerializable]
        public class WiresActionMessage : BoundUserInterfaceMessage
        {
            public readonly Guid Guid;
            public readonly WiresAction Action;
            public WiresActionMessage(Guid guid, WiresAction action)
            {
                Guid = guid;
                Action = action;
            }
        }
    }
}
