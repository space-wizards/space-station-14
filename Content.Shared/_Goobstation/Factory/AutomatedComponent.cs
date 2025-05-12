// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 deltanedas <@deltanedas:kde.org>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Shared._Goobstation.Factory;

/// <summary>
/// Component added to machines with <see cref="AutomationSlotsComponent"/> to enable their ports for linking.
/// They can then be automated with things like a <see cref="RoboticArmComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AutomatedComponent : Component;
