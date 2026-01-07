// SPDX-FileCopyrightText: 2025 Skye <57879983+Rainbeon@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 kbarkevich <24629810+kbarkevich@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
// SPDX-FileCopyrightText: 2025 Terkala <appleorange64@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later OR MIT

using Robust.Shared.GameStates;

namespace Content.Shared.BloodCult.Components;

/// <summary>
/// Marks melee weapons that should not injure fellow cult members.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BloodCultMeleeWeaponComponent : Component
{
}

