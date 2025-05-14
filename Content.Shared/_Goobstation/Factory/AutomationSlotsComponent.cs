// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 deltanedas <@deltanedas:kde.org>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Goobstation.Factory.Slots;
using Robust.Shared.GameStates;

namespace Content.Shared._Goobstation.Factory;

/// <summary>
/// Adds slots to an entity that can be controlled by automation machines if it also has <see cref="AutomationComponent"/>.
/// Slots using <see cref="AutomationSlot"/> can provide or accept items.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(AutomationSystem))]
public sealed partial class AutomationSlotsComponent : Component
{
    /// <summary>
    /// All input slots that can be automated.
    /// </summary>
    [DataField(required: true)]
    public List<AutomationSlot> Slots = new();
}
