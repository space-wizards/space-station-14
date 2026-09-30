using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Changeling.Components;

/// <summary>
/// Allows the changeling to spawn dummy chameleon clothing items that will transform with them,
/// mimicing the equipment of the stored disguise.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ChangelingFleshClothingAbilityComponent : Component
{
    /// <summary>
    /// Is the ability currently enabled?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// The alert for showing if the ability is active and for toggling it.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> AlertId = "ChangelingFleshClothing";

    /// <summary>
    /// The chameleon clothing items to spawn into any empty slots if the changeling transformed.
    /// These need <see cref="ChangelingFleshClothingComponent"/> so that they will change their visuals according to the identity we transformed into.
    /// The key of the dictionary is the corresponding inventory slot.
    /// </summary>
    [DataField]
    public Dictionary<string, EntProtoId> ClothingPrototypes = new();
}
