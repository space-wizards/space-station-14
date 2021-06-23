using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Server.NodeContainer;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Atmos.Piping.Unary.EntitySystems
{
    [UsedImplicitly]
    public class GasTankSystem : EntitySystem
    {
        private const float TimerDelay = 0.5f;
        private float _timer = 0f;

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
            tankNode.Volume = tank.InitialMixture.Volume;
            tankNode.Air.Volume = tank.InitialMixture.Volume;
            tankNode.Air.Temperature = tank.InitialMixture.Temperature;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            _timer += frameTime;

            if (_timer < TimerDelay) return;
            _timer -= TimerDelay;

            var atmosphereSystem = Get<AtmosphereSystem>();

            foreach (var gasTank in EntityManager.ComponentManager.EntityQuery<GasTankComponent>(true))
            {
                atmosphereSystem.React(gasTank.Air, gasTank);
                gasTank.CheckStatus();
                gasTank.UpdateUserInterface();
            }
        }
    }
}
