using Content.Server.GameObjects.Components.Chemistry;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Server.Atmos
{
    [RegisterComponent]
    public class GasSprayerComponent : Component, IAfterInteract
    {
        [Dependency] private readonly IServerEntityManager _serverEntityManager = default!;

        //TODO: create a function that can create a gas based on a solution mix
        public override string Name => "GasSprayer";

        private string _spraySound;
        private string _sprayType;
        private string _fuelType;
        private string _fuelName;
        private int _fuelCost;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _spraySound, "spraySound", string.Empty);
            serializer.DataField(ref _sprayType, "sprayType", string.Empty);
            serializer.DataField(ref _fuelType, "fuelType", string.Empty);
            serializer.DataField(ref _fuelName, "fuelName", "fuel");
            serializer.DataField(ref _fuelCost, "fuelCost", 50);
        }

        public void AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (!Owner.TryGetComponent(out SolutionContainerComponent tank))
                return;

            if (tank.Solution.GetReagentQuantity(_fuelType) == 0)
            {
                Owner.PopupMessage(eventArgs.User,
                    Loc.GetString("{0:theName} is out of {1}!", Owner, _fuelName));
            }
            else
            {
                tank.TryRemoveReagent(_fuelType, ReagentUnit.New(_fuelCost));

                var playerPos = eventArgs.User.Transform.Coordinates;
                var direction = (eventArgs.ClickLocation.Position - playerPos.Position).Normalized;
                playerPos.Offset(direction/2);

                var spray = _serverEntityManager.SpawnEntity(_sprayType, playerPos);
                spray.GetComponent<AppearanceComponent>()
                    .SetData(ExtinguisherVisuals.Rotation, direction.ToAngle().Degrees);
                spray.GetComponent<GasVaporComponent>().StartMove(direction, 5);

                EntitySystem.Get<AudioSystem>().PlayFromEntity(_spraySound, Owner);
            }
        }
    }
}
