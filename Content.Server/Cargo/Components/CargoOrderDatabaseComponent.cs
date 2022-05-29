using Content.Shared.Cargo.Components;

namespace Content.Server.Cargo.Components
{
    [RegisterComponent]
    public sealed class CargoOrderDatabaseComponent : SharedCargoOrderDatabaseComponent
    {
        public CargoOrderDatabase? Database { get; set; }
        public bool ConnectedToDatabase => Database != null;

        protected override void Initialize()
        {
            base.Initialize();

            Database = EntitySystem.Get<CargoSystem>().StationOrderDatabase;
        }

        public override ComponentState GetComponentState()
        {
            if (!ConnectedToDatabase)
                return new CargoOrderDatabaseState(null);
            return new CargoOrderDatabaseState(Database?.GetOrders());
        }
    }
}
