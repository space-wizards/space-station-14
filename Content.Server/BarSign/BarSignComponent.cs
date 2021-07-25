using System.Linq;
using Content.Server.Power.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.BarSign
{
    [RegisterComponent]
    public class BarSignComponent : Component
    {
        public override string Name => "BarSign";

        [DataField("current")] [ViewVariables(VVAccess.ReadOnly)]
        public string? CurrentSign;
    }
}
