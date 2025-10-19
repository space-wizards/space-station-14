// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.GameStates;

namespace Content.Shared.Overlays;

/// <summary>
///     This component allows you to see the hungriness of mobs.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ShowHungerIconsComponent : Component { }
