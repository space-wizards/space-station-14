using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration
{
    [Serializable, NetSerializable]
    public sealed partial class SetOutfitEuiState : EuiStateBase
    {
        public NetEntity TargetNetEntity;
    }
}

