// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.GameStates;

namespace Content.Shared.Morgue.Components;

/// <summary>
/// Used to track actively cooking crematoriums.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActiveCrematoriumComponent : Component;
