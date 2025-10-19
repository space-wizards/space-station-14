// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.GameStates;

namespace Content.Shared.Survivor.Components;

/// <summary>
///     Component to keep track of which entities are a Survivor antag.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SurvivorComponent : Component;
