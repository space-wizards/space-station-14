using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Disposal.Components
{
    public abstract class SharedDisposalMailingUnitComponent : SharedDisposalUnitComponent
    {
        public override string Name => "DisposalMailingUnit";

        public const string TAGS_MAIL = "mail";

        public const string NET_TAG = "tag";
        public const string NET_SRC = "src";
        public const string NET_TARGET = "target";
        public const string NET_CMD_SENT = "mail_sent";
        public const string NET_CMD_REQUEST = "get_mailer_tag";
        public const string NET_CMD_RESPONSE = "mailer_tag";

        [Serializable, NetSerializable]
        public new enum UiButton : byte
        {
            Eject,
            Engage,
            Power
        }

        [Serializable, NetSerializable]
        public class DisposalMailingUnitBoundUserInterfaceState : BoundUserInterfaceState, IEquatable<DisposalMailingUnitBoundUserInterfaceState>, ICloneable
        {
            public readonly string UnitName;
            public readonly string UnitState;
            public readonly float Pressure;
            public readonly bool Powered;
            public readonly bool Engaged;
            public readonly string Tag;
            public readonly List<string> Tags;
            public readonly string? Target;

            public DisposalMailingUnitBoundUserInterfaceState(string unitName, string unitState, float pressure, bool powered,
                bool engaged, string tag, List<string> tags, string? target)
            {
                UnitName = unitName;
                UnitState = unitState;
                Pressure = pressure;
                Powered = powered;
                Engaged = engaged;
                Tag = tag;
                Tags = tags;
                Target = target;
            }

            public object Clone()
            {
                return new DisposalMailingUnitBoundUserInterfaceState(UnitName, UnitState, Pressure, Powered, Engaged, Tag, (List<string>)Tags.Clone(), Target);
            }

            public bool Equals(DisposalMailingUnitBoundUserInterfaceState? other)
            {
                if (other is null) return false;
                if (ReferenceEquals(this, other)) return true;
                return UnitName == other.UnitName &&
                       UnitState == other.UnitState &&
                       Powered == other.Powered &&
                       Engaged == other.Engaged &&
                       Pressure.Equals(other.Pressure) &&
                       Tag == other.Tag &&
                       Target == other.Target;
            }
        }

        /// <summary>
        ///     Message data sent from client to server when a mailing unit ui button is pressed.
        /// </summary>
        [Serializable, NetSerializable]
        public new class UiButtonPressedMessage : BoundUserInterfaceMessage
        {
            public readonly UiButton Button;

            public UiButtonPressedMessage(UiButton button)
            {
                Button = button;
            }
        }

        /// <summary>
        ///     Message data sent from client to server when the mailing units target is updated.
        /// </summary>
        [Serializable, NetSerializable]
        public class UiTargetUpdateMessage : BoundUserInterfaceMessage
        {
            public readonly string? Target;

            public UiTargetUpdateMessage(string? target)
            {
                Target = target;
            }
        }

        [Serializable, NetSerializable]
        public enum DisposalMailingUnitUiKey
        {
            Key
        }
    }
}
