using System;
using System.Collections.Generic;

namespace Content.Shared.Medical.Disease;

/// <summary>
/// Enumeration describing disease transmission vectors.
/// TODO: only the Contact works and Airborne.
/// </summary>
public enum DiseaseSpreadFlags
{
    NonContagious = 0,
    Airborne = 1 << 0,
    Contact = 1 << 1,
    Blood = 1 << 2,
    Special = 1 << 3,
}

/// <summary>
/// Enumeration describing disease stealth behavior flags.
/// TODO:
/// - None: default behavior
/// - Hidden: do not show in HUD
/// - VeryHidden: hide from HUD, diagnoser, and health analyzer
/// - HiddenTreatment: hide treatment steps in diagnoser
/// - HiddenStage: hide stage in diagnoser and health analyzer
/// </summary>
[Flags]
public enum DiseaseStealthFlags
{
    None = 0,
    Hidden = 1 << 0,
    VeryHidden = 1 << 1,
    HiddenTreatment = 1 << 2,
    HiddenStage = 1 << 3,
}

/// <summary>
/// Global configuration for PPE/internals effectiveness used by infection checks.
/// Centralized here to avoid prototype-level duplication and keep balance consistent.
/// </summary>
public static class DiseaseEffectiveness
{
    // Airborne protection
    public const float InternalsMultiplier = 0.25f;

    public static readonly (string Slot, float Multiplier)[] AirborneSlots = new[]
    {
        ("mask", 0.7f),
        ("head", 0.85f),
        ("eyes", 0.9f),
    };

    // Contact protection
    public static readonly (string Slot, float Multiplier)[] ContactSlots = new[]
    {
        ("gloves", 0.7f),
        ("shoes", 0.8f),
        ("outerClothing", 0.85f),
        ("uniform", 0.9f),
    };

    // Foot residue deposit modifiers
    public static readonly (string Slot, float Multiplier)[] FootResidueSlots = new[]
    {
        ("shoes", 0.5f),
    };
}
