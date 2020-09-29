using Content.Server.Cargo;
using Content.Shared.GameObjects.Components.Cargo;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.Components.Cargo
{
    [RegisterComponent]
    public class CargoOrderDatabaseComponent : SharedCargoOrderDatabaseComponent
    {
        [Dependency] private readonly ICargoOrderDataManager _cargoOrderDataManager = default!;

        public CargoOrderDatabase Database { get; set; }
        public bool ConnectedToDatabase => Database != null;

        public override void Initialize()
        {
            base.Initialize();

            _cargoOrderDataManager.AddComponent(this);
        }

        public override ComponentState GetComponentState()
        {
            if (!ConnectedToDatabase)
                return new CargoOrderDatabaseState(null);
            return new CargoOrderDatabaseState(Database.GetOrders());
        }
    }
}
