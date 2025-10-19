// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.GameStates;

namespace Content.Shared.Administration.Components;

/// <summary>
/// Flips the target's sprite on its head, so they do a headstand.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HeadstandComponent : Component;
