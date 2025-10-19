// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

namespace Content.Server.Atmos.Components;

/// <summary>
/// Component that can be used to add (or remove) fire stacks when used as a melee weapon.
/// </summary>
[RegisterComponent]
public sealed partial class IgniteOnMeleeHitComponent : Component
{
    [DataField]
    public float FireStacks { get; set; }
}
