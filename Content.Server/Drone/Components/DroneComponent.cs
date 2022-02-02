using System.Collections.Generic;
using Content.Server.Storage;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;


namespace Content.Server.Drone.Components
{
    [RegisterComponent, ComponentProtoName("Drone")]
    public sealed class DroneComponent : Component
    {
        [DataField("tools")] public List<EntitySpawnEntry> Tools = new();


        public bool alreadyAwoken = false;
    }
}
