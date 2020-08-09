using Content.Server.Interfaces;
using Content.Shared.GameObjects.Components.Pointing;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;


namespace Content.Server.Atmos
{
    [RegisterComponent]
    public class GasSprayerComponent: Component, IAfterInteract
    {
#pragma warning disable 649
        [Dependency] private readonly IServerNotifyManager _notifyManager = default!;
        [Dependency] private readonly IServerEntityManager _serverEntityManager = default!;
#pragma warning restore 649

        public override string Name => "GasSprayer";
        public void AfterInteract(AfterInteractEventArgs eventArgs)
        {

            var playerPos = eventArgs.User.Transform.GridPosition;
            var direction = (eventArgs.ClickLocation.Position - playerPos.Position).Normalized;
            playerPos.Offset(direction);

            var spray =_serverEntityManager.SpawnEntity("ExtinguisherSpray",playerPos);

            if(!spray.TryGetComponent(out AppearanceComponent appearance))
            {
                return;
            }

            appearance.SetData(RoguePointingArrowVisuals.Rotation,direction.ToAngle().Degrees);

            //Todo: Parameterize into prototype
            spray.GetComponent<GasVaporComponent>().StartMove(direction,5);

            return;
        }
    }
}
