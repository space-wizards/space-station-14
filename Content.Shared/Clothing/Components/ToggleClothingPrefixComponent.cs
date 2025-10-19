// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.GameStates;

namespace Content.Shared.Clothing.Components;

/// <summary>
/// Handles the changes to ClothingComponent.EquippedPrefix when toggled.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ToggleClothingPrefixComponent : Component
{
    /// <summary>
    /// Clothing's EquippedPrefix when activated.
    /// </summary>
    [DataField]
    public string? PrefixOn = "on";

    /// <summary>
    /// Clothing's EquippedPrefix when deactivated.
    /// </summary>
    [DataField]
    public string? PrefixOff;
}
