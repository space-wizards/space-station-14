using System;
using System.Collections.Generic;

namespace Content.Server.Utility
{
    class AccessHelper
    {
        /// <summary>
        /// Dictionary contaning departamental access string.<br/>
        /// A substitue because /area is gone. <b>Please update this accordingly!</b>
        /// </summary>
        private readonly Dictionary<DoorSector, string[]> _doorSectorDict = new Dictionary<DoorSector, string[]>(){
            { DoorSector.Security, new string[]{ "Security", "Brig" } },
            { DoorSector.Command, new string[]{ "Command", "Bridge" } },
            { DoorSector.Engine, new string[]{ "Engineering" } },
            { DoorSector.Medical, new string[]{ "Medical" } },
            { DoorSector.Cargo, new string[]{ "Cargo", "Mining" } }, // issue with mining doors: greytide may space the mining base (assuming lavaland)
            { DoorSector.Science, new string[]{ "Science", "Research And Development" } }
        };

        /// <summary>
        /// Gets the access list assosiated with the <see cref="DoorSector"/>.
        /// </summary>
        /// <param name="sector"></param>
        /// <param name="doorNames"></param>
        /// <returns></returns>
        public bool TryGetDepartmentDoorNames(DoorSector sector, out string[] doorNames)
        {
            return _doorSectorDict.TryGetValue(sector, out doorNames);
        }

        public enum DoorSector
        {
            Security,
            Command,
            Engine,
            Medical,
            Cargo,
            Science
        }
    }
}
