// SPDX-FileCopyrightText: 2025 Terkala <appleorange64@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later OR MIT

using Robust.Shared.GameStates;

namespace Content.Shared.BloodCult.Components;

/// <summary>
/// Marks an entity as having a body container that should be ejected on destruction
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class JuggernautBodyContainerComponent : Component
{
    /// <summary>
    /// The container ID that holds the body
    /// </summary>
    [DataField]
    public string ContainerId = "juggernaut_body_container";
}

