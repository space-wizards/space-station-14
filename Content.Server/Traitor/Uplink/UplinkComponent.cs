using Content.Shared.Traitor.Uplink;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.Traitor.Uplink.Components
{
    [RegisterComponent]
    public class UplinkComponent : Component
    {
        public override string Name => "Uplink";

        [ViewVariables] public UplinkAccount? SyndicateUplinkAccount;
    }
}
