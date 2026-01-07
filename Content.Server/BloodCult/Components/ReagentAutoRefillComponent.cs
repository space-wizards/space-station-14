// SPDX-FileCopyrightText: 2025 Terkala <appleorange64@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later OR MIT

using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Server.BloodCult.Components;

/// <summary>
/// Automatically refills a specific reagent in a solution over time.
/// Used by cult daggers to regenerate their Edge Essentia.
/// </summary>
[RegisterComponent]
public sealed partial class ReagentAutoRefillComponent : Component
{
    /// <summary>
    /// The solution to refill
    /// </summary>
    [DataField(required: true)]
    public string Solution = string.Empty;

    /// <summary>
    /// The reagent to refill (e.g., "EdgeEssentia")
    /// </summary>
    [DataField(required: true)]
    public ProtoId<ReagentPrototype> Reagent = string.Empty;

    /// <summary>
    /// How many units to refill per second
    /// </summary>
    [DataField]
    public float RefillRate = 0.5f;

    /// <summary>
    /// Maximum amount of this reagent to maintain in the solution
    /// </summary>
    [DataField]
    public float MaxAmount = 15f;
}

