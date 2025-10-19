// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.GameStates;

namespace Content.Shared.Eye.Blinding.Components;

/// <summary>
///     Blinds a person when an item with this component is equipped to the eye, head, or mask slot.
/// </summary>
[RegisterComponent]
[NetworkedComponent]
public sealed partial class BlindfoldComponent : Component
{
}
