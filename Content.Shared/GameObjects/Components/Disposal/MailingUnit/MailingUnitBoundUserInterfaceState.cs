using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using static Content.Shared.GameObjects.Components.Disposal.DisposalUnit.DisposalUnitBoundUserInterfaceState;

namespace Content.Shared.GameObjects.Components.Disposal.MailingUnit
{
    [Serializable, NetSerializable]
    public class MailingUnitBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly string UnitName;
        public readonly PressureState UnitState;
        public readonly bool Powered;
        public readonly bool Engaged;
        public readonly string Tag;
        public readonly IReadOnlyList<string> Tags;
        public readonly string Target;

        public MailingUnitBoundUserInterfaceState(string unitName, PressureState unitState, bool powered,
            bool engaged, string tag, IReadOnlyList<string> tags, string target)
        {
            UnitName = unitName;
            UnitState = unitState;
            Powered = powered;
            Engaged = engaged;
            Tag = tag;
            Tags = tags;
            Target = target;
        }
    }
}
