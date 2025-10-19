// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.Fax.Components;

/// <summary>
/// A fax component which stores a damage specifier for attempting to fax a mob.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FaxecuteComponent : Component
{

    /// <summary>
    /// Type of damage dealt when entity is faxecuted.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public DamageSpecifier Damage = new();
}

