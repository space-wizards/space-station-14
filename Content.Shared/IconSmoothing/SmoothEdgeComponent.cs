// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.GameStates;

namespace Content.Shared.IconSmoothing;

/// <summary>
/// Applies an edge sprite to <see cref="IconSmoothComponent"/> for non-smoothed directions.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SmoothEdgeComponent : Component
{

}
