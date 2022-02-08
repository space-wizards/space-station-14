using Content.Shared.Cargo.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Players;

namespace Content.Server.Cargo.Components
{
    [RegisterComponent]
    public class CargoOrderDatabaseComponent : SharedCargoOrderDatabaseComponent
    {
        public CargoOrderDatabase? Database { get; set; }
        public bool ConnectedToDatabase => Database != null;

        protected override void Initialize()
        {
            base.Initialize();

            Database = EntitySystem.Get<CargoConsoleSystem>().StationOrderDatabase;
        }

        public override ComponentState GetComponentState()
        {
            if (!ConnectedToDatabase)
                return new CargoOrderDatabaseState(null);
            return new CargoOrderDatabaseState(Database?.GetOrders());
        }
    }
}
