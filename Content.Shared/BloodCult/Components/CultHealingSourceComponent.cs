// SPDX-FileCopyrightText: 2025 Skye <57879983+Rainbeon@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 kbarkevich <24629810+kbarkevich@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

namespace Content.Shared.BloodCult.Components;

/// <summary>
///     Heal all cultists in range.
/// </summary>
[RegisterComponent]
public sealed partial class CultHealingSourceComponent : Component
{
    /// <summary>
    ///     Healing intensity in center of the source in damage healed per second.
    ///     From there rays will travel over distance and lose intensity
    ///     when hit blocker.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("intensity")]
    public float Intensity = 1.5f;

    /// <summary>
    ///     Defines how fast rays will loose intensity over
    ///     distance. The bigger the value, the shorter the
    ///     range of the healing source will be.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("slope")]
    public float Slope = 0.4f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled = true;
}
