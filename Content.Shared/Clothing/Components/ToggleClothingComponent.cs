using Content.Shared.Actions;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Toggleable;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Clothing.Components;

/// <summary>
/// Clothing that can be enabled and disabled with an action.
/// Requires <see cref="ItemToggleComponent"/>.
/// </summary>
/// <remarks>
/// Not to be confused with <see cref="ToggleableClothingComponent"/> for hardsuit helmets and such.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ToggleClothingSystem))]
public sealed partial class ToggleClothingComponent : Component
{
    /// <summary>
    /// The action to add when equipped, even if not worn.
    /// This must raise <see cref="ToggleActionEvent"/> to then get handled.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId<InstantActionComponent> Action = string.Empty;

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    /// <summary>
    /// If true, automatically disable the clothing after unequipping it.
    /// </summary>
    [DataField]
    public bool DisableOnUnequip;
}

/// <summary>
/// Raised on the clothing when being equipped to see if it should add the action.
/// </summary>
[ByRefEvent]
public record struct ToggleClothingCheckEvent(EntityUid User, bool Cancelled = false);
