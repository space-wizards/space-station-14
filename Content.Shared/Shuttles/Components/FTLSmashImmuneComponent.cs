// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.GameStates;

namespace Content.Shared.Shuttles.Components;

/// <summary>
/// Makes the entity immune to FTL arrival landing AKA smimsh.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class FTLSmashImmuneComponent : Component;
