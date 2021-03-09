using Content.Server.Cargo;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Cargo;
using Robust.Shared.GameObjects;
using Robust.Shared.Players;

namespace Content.Server.GameObjects.Components.Cargo
{
    [RegisterComponent]
    public class CargoOrderDatabaseComponent : SharedCargoOrderDatabaseComponent
    {
        public CargoOrderDatabase Database { get; set; }
        public bool ConnectedToDatabase => Database != null;

        public override void Initialize()
        {
            base.Initialize();

            Database = EntitySystem.Get<CargoConsoleSystem>().StationOrderDatabase;
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            if (!ConnectedToDatabase)
                return new CargoOrderDatabaseState(null);
            return new CargoOrderDatabaseState(Database.GetOrders());
        }
    }
}
