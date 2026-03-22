using Robust.Shared.Prototypes;

namespace Content.Server.Photography;
// since camera is taken...
/// <summary>
/// marks an entity as able to take pictures (when you smash other entities with it)
/// </summary>
[RegisterComponent]
public sealed partial class PictureTakerComponent: Component
{
    [DataField]
    public List<EntProtoId> Photographs;
}
