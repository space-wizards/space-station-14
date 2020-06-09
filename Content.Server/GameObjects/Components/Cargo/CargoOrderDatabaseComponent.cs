using Content.Server.Cargo;

namespace Content.Server.GameObjects.Components.Cargo
{
    [RegisterComponent]
    public class CargoOrderDatabaseComponent : SharedCargoOrderDatabaseComponent
    {
#pragma warning disable 649
        [Dependency] private readonly ICargoOrderDataManager _cargoOrderDataManager;
#pragma warning restore 649

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
