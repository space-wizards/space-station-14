using Content.Shared.EntityTable;
using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Prototypes;

namespace Content.Shared.Photography;
// since camera is taken...
/// <summary>
/// marks an entity as able to take pictures (when you smash other entities with it)
/// </summary>
[RegisterComponent]
[AutoGenerateComponentState]
public sealed partial class PictureTakerComponent: Component
{
    /// <summary>
    /// The entities that will be instanced & given a PhotographComponent to when the PictureTakerComponent's entity is used to bash something. Hopefully, only one of these will be chosen, at random, each time.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityTableSelector Photographs = default!;
}
