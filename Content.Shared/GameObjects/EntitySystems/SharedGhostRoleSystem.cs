#nullable enable
using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.EntitySystems
{
    public abstract class SharedGhostRoleSystem : EntitySystem
    {

    }

    [Serializable, NetSerializable]
    public class GhostRole
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public EntityUid Id;
    }
}
