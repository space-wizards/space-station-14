// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.GameStates;

namespace Content.Shared.Light.Components;

/// <summary>
/// Enables / disables pointlight whenever entities are contacting with it
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class LightOnCollideComponent : Component
{
}
