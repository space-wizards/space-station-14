using Content.Server.Heretic.EntitySystems;

namespace Content.Server.Heretic.Components;

[RegisterComponent, Access(typeof(MansusGraspSystem))]
public sealed partial class MansusGraspComponent : Component
{
    [DataField] public string? Path = null;
}
