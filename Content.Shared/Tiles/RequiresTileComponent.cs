// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.GameStates;

namespace Content.Shared.Tiles;

/// <summary>
/// Deletes the entity if the tile changes from under it. Used for visual effects.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RequiresTileComponent : Component
{

}
