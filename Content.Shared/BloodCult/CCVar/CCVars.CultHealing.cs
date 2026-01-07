// SPDX-FileCopyrightText: 2025 Skye <57879983+Rainbeon@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 kbarkevich <24629810+kbarkevich@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    ///     What is the smallest dose of healing that can be received by object.
    ///     Extremely small values may impact performance.
    /// </summary>
    public static readonly CVarDef<float> CultHealingMinIntensity =
        CVarDef.Create("cult_healing.min_intensity", 0.1f, CVar.SERVERONLY);

    /// <summary>
    ///     Rate of cult healing system update in seconds.
    /// </summary>
    public static readonly CVarDef<float> CultHealingGridcastUpdateRate =
        CVarDef.Create("cult_healing.gridcast.update_rate", 1.0f, CVar.SERVERONLY);

    /// <summary>
    ///     If both healing source and receiver are placed on same grid, ignore grids between them.
    ///     May get inaccurate result in some cases, but greatly boost performance in general.
    /// </summary>
    public static readonly CVarDef<bool> CultHealingGridcastSimplifiedSameGrid =
        CVarDef.Create("cult_healing.gridcast.simplified_same_grid", true, CVar.SERVERONLY);

    /// <summary>
    ///     Max distance that a cult healing ray can travel in meters.
    /// </summary>
    public static readonly CVarDef<float> CultHealingGridcastMaxDistance =
        CVarDef.Create("cult_healing.gridcast.max_distance", 10f, CVar.SERVERONLY);
}
