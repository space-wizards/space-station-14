using Content.Shared.Eui;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared.Administration
{
    [Serializable, NetSerializable]
    public class SetOutfitEuiState : EuiStateBase
    {
        public string TargetEntityId;
    }
}
