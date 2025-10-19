// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.GameStates;

namespace Content.Shared.PowerCell.Components;

/// <summary>
/// Integrate PowerCellDraw and ItemToggle.
/// Make toggling this item require power, and deactivates the item when power runs out.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ToggleCellDrawComponent : Component;
