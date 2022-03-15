using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Warps
{
    /// <summary>
    /// Allows ghosts etc to warp to this entity by name.
    /// </summary>
    [RegisterComponent, ComponentProtoName("WarpPoint")]
    public sealed class WarpPointComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)] [DataField("location")] public string? Location { get; set; }
    }
}
