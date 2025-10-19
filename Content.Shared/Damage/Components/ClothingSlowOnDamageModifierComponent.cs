// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.GameStates;

namespace Content.Shared.Damage.Components;

/// <summary>
/// This is used for a clothing item that modifies the slowdown from taking damage.
/// Used for entities with <see cref="SlowOnDamageComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SlowOnDamageSystem))]
public sealed partial class ClothingSlowOnDamageModifierComponent : Component
{
    /// <summary>
    /// A coefficient modifier for the slowdown
    /// </summary>
    [DataField]
    public float Modifier = 1;
}
