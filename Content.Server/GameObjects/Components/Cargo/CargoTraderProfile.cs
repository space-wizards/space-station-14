using System.Collections.Generic;

namespace Content.Server.GameObjects.Components.Cargo
{
    public class CargoTraderProfile
    {
        public readonly string Name;

        public List<CargoRequest> ActiveRequests = new();

        public CargoTraderProfile(string name)
        {
            Name = name;
        }
    }
}
