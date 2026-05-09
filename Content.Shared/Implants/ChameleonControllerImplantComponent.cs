using Content.Shared.Actions;
using Content.Shared.Inventory;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Implants;

/// <summary>
///     Will allow anyone implanted with the implant to have more control over their chameleon clothing and items.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ChameleonControllerImplantComponent : Component;

/// <summary>
///     This is sent when someone clicks on the hud icon and will open the menu.
/// </summary>
public sealed partial class ChameleonControllerOpenMenuEvent : InstantActionEvent;

[Serializable, NetSerializable]
public enum ChameleonControllerKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class ChameleonControllerBuiState : BoundUserInterfaceState;

/// <summary>
///     Triggered when the user clicks on a job in the menu.
/// </summary>
[Serializable, NetSerializable]
public sealed class ChameleonControllerSelectedOutfitMessage(ProtoId<ChameleonOutfitPrototype> selectedOutfit) : BoundUserInterfaceMessage
{
    public readonly ProtoId<ChameleonOutfitPrototype> SelectedChameleonOutfit = selectedOutfit;
}

/// <summary>
///     This event is raised on clothing when the chameleon controller wants it to change sprite based off selecting an
///      outfit.
/// </summary>
/// <param name="ChameleonOutfit">The outfit being switched to.</param>
/// <param name="CustomRoleLoadout">The users custom loadout for the chameleon outfits job.</param>
/// <param name="DefaultRoleLoadout">The default loadout for the chameleon outfits job.</param>
/// <param name="JobStartingGearPrototype">The starting gear of the chameleon outfits job.</param>
[ByRefEvent]
public record struct ChameleonControllerOutfitSelectedEvent(
    ChameleonOutfitPrototype ChameleonOutfit,
    RoleLoadout? CustomRoleLoadout,
    RoleLoadout? DefaultRoleLoadout,
    StartingGearPrototype? JobStartingGearPrototype,
    StartingGearPrototype? StartingGearPrototype
) : IInventoryRelayEvent
{
    SlotFlags IInventoryRelayEvent.TargetSlots => SlotFlags.WITHOUT_POCKET;
}
