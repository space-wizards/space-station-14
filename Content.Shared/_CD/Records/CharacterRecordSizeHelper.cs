using System;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;

namespace Content.Shared._CD.Records;

/// <summary>
/// Helper utilities for deriving record metadata from a character profile.
/// </summary>
public static class CharacterRecordSizeHelper
{
    public static bool TryCalculateMetrics(
        HumanoidCharacterProfile? profile,
        IPrototypeManager prototypes,
        out int heightCentimeters,
        out int weightKilograms)
    {
        heightCentimeters = 0;
        weightKilograms = 0;

        if (profile == null)
            return false;

        // Species could have been removed or renamed; guard against missing prototypes.
        if (!prototypes.TryIndex<SpeciesPrototype>(profile.Species, out var species))
            return false;

        var heightScale = profile.Appearance.Height;
        var widthScale = profile.Appearance.Width;

        // Convert proportions back into concrete centimeter/kilogram figures using the species defaults.
        var height = species.StandardSize * (heightScale - 1f) * 2f + species.StandardSize;
        var weight = species.StandardWeight +
                     species.StandardDensity * (widthScale * heightScale * heightScale - 1f);

        // Clamp to configured profile limits so downstream DB/UI consumers cannot explode.
        heightCentimeters = Math.Clamp((int) Math.Round(height), 0, PlayerProvidedCharacterRecords.MaxHeight);
        weightKilograms = Math.Clamp((int) Math.Round(weight), 0, PlayerProvidedCharacterRecords.MaxWeight);
        return true;
    }

    public static PlayerProvidedCharacterRecords WithCalculatedMetrics(
        PlayerProvidedCharacterRecords records,
        HumanoidCharacterProfile? profile,
        IPrototypeManager prototypes)
    {
        // Only mutate the record when we successfully derived fresh numbers.
        return TryCalculateMetrics(profile, prototypes, out var heightCm, out var weightKg)
            ? records.WithHeight(heightCm).WithWeight(weightKg)
            : records;
    }
}
