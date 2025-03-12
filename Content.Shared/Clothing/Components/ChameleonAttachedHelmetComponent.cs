using Content.Shared.Clothing.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Clothing.Components;

/// <summary>
///     This component indicates that this clothing usually comes with a matching helmet. This allows a
///     <see cref="ChameleonClothingComponent"/> that also has a <see cref="ToggleableClothingComponent"/>
///     to attach the matching helmet to itself.
/// </summary>
[Access(typeof(SharedChameleonClothingSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ChameleonAttachedHelmetComponent : Component
{
    /// <summary>
    ///     Default clothing entity prototype that comes with this prototype.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId ClothingPrototype = default!;
}
