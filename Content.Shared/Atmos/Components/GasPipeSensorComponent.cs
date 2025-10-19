// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Components;

/// <summary>
/// Entities with component will be queried against for their
/// atmos monitoring data on atmos monitoring consoles
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GasPipeSensorComponent : Component;
