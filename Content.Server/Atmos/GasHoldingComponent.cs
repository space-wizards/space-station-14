using Content.Server.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Atmos
{
    [RegisterComponent]
    public class GasHoldingComponent: Component,IGasMixtureHolder
    {

#pragma warning disable 649
        [Dependency] private readonly IServerNotifyManager _notifyManager = default!;
        [Dependency] private readonly IServerEntityManager _serverEntityManager = default!;
#pragma warning restore 649

        public override string Name => "GasHolder";
        public GasMixture Air { get; set; }
        public bool AssumeAir(GasMixture giver)
        {
            throw new System.NotImplementedException();
        }
    }
}
