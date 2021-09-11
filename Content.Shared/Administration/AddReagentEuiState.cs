using System;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration
{
    [Serializable, NetSerializable]
    public sealed class AddReagentEuiState : EuiStateBase
    {
        public ReagentUnit MaxVolume;
        public ReagentUnit CurVolume;
    }

    public static class AddReagentEuiMsg
    {
        [Serializable, NetSerializable]
        public sealed class Close : EuiMessageBase
        {

        }

        [Serializable, NetSerializable]
        public sealed class DoAdd : EuiMessageBase
        {
            public bool CloseAfter;
            public ReagentUnit Amount;
            public string ReagentId = string.Empty;
        }
    }
}
