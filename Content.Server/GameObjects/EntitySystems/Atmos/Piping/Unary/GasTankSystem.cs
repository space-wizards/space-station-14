using Content.Server.GameObjects.Components.Atmos;
using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems.Atmos.Piping.Unary
{
    [UsedImplicitly]
    public class GasTankSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasTankComponent, ComponentStartup>(OnTankStartup);
        }

        private void OnTankStartup(EntityUid uid, GasTankComponent tank, ComponentStartup args)
        {
            if (!ComponentManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer))
                return;

            if (!nodeContainer.TryGetNode(tank.TankName, out PipeNode? tankNode))
                return;

            // Create a pipenet if we don't have one already.
            tankNode.TryAssignGroupIfNeeded();
            tankNode.AssumeAir(tank.InitialMixture);
            tankNode.Air.Temperature = tank.InitialMixture.Temperature;
        }
    }
}
