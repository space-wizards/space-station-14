// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

/// <summary>
/// Is this entity always considered to be touching a wall?
/// i.e. when weightless they're floaty but still have free movement.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MovementAlwaysTouchingComponent : Component
{

}
