using System;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Eui;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration
{
    [Serializable, NetSerializable]
    public sealed class AdminAddReagentEuiState : EuiStateBase
    {
        public FixedPoint2 MaxVolume;
        public FixedPoint2 CurVolume;
    }

    public static class AdminAddReagentEuiMsg
    {
        [Serializable, NetSerializable]
        public sealed class Close : EuiMessageBase
        {

        }

        [Serializable, NetSerializable]
        public sealed class DoAdd : EuiMessageBase
        {
            public bool CloseAfter;
            public FixedPoint2 Amount;
            public string ReagentId = string.Empty;
        }
    }
}
