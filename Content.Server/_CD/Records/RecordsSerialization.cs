using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using Content.Server.Database;
using Content.Shared._CD.Records;

namespace Content.Server._CD.Records;

/// <summary>
/// Helper helpers for translating profile record entries to the database representation.
/// </summary>
public static class RecordsSerialization
{
    private static int DeserializeInt(JsonElement element, string key, int fallback)
    {
        if (element.TryGetProperty(key, out var property) && property.TryGetInt32(out var value))
            return value;

        return fallback;
    }

    private static bool DeserializeBool(JsonElement element, string key, bool fallback)
    {
        if (!element.TryGetProperty(key, out var value))
            return fallback;

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => fallback,
        };
    }

    [return: NotNullIfNotNull(nameof(fallback))]
    private static string? DeserializeString(JsonElement element, string key, string? fallback)
    {
        if (!element.TryGetProperty(key, out var value))
            return fallback;

        if (value.ValueKind == JsonValueKind.String)
            return value.GetString() ?? fallback;

        return fallback;
    }

    private static List<PlayerProvidedCharacterRecords.RecordEntry> DeserializeEntries(
        List<CDModel.CharacterRecordEntry> entries,
        CDModel.DbRecordEntryType type)
    {
        // Preserve the insertion order from the database so consoles show entries chronologically.
        return entries
            .Where(entry => entry.Type == type)
            .OrderBy(entry => entry.Id)
            .Select(entry => new PlayerProvidedCharacterRecords.RecordEntry(entry.Title, entry.Involved, entry.Description))
            .ToList();
    }

    /// <summary>
    /// Translate the stored JSON into <see cref="PlayerProvidedCharacterRecords"/>.
    /// Missing fields are filled with defaults while unknown fields are ignored.
    /// </summary>
    public static PlayerProvidedCharacterRecords Deserialize(JsonDocument json, List<CDModel.CharacterRecordEntry> entries)
    {
        var element = json.RootElement;
        var defaults = PlayerProvidedCharacterRecords.DefaultRecords();
        return new PlayerProvidedCharacterRecords(
            height: DeserializeInt(element, nameof(defaults.Height), defaults.Height),
            weight: DeserializeInt(element, nameof(defaults.Weight), defaults.Weight),
            emergencyContactName: DeserializeString(element, nameof(defaults.EmergencyContactName), defaults.EmergencyContactName),
            hasWorkAuthorization: DeserializeBool(element, nameof(defaults.HasWorkAuthorization), defaults.HasWorkAuthorization),
            identifyingFeatures: DeserializeString(element, nameof(defaults.IdentifyingFeatures), defaults.IdentifyingFeatures),
            allergies: DeserializeString(element, nameof(defaults.Allergies), defaults.Allergies),
            drugAllergies: DeserializeString(element, nameof(defaults.DrugAllergies), defaults.DrugAllergies),
            postmortemInstructions: DeserializeString(element, nameof(defaults.PostmortemInstructions), defaults.PostmortemInstructions),
            medicalEntries: DeserializeEntries(entries, CDModel.DbRecordEntryType.Medical),
            securityEntries: DeserializeEntries(entries, CDModel.DbRecordEntryType.Security),
            employmentEntries: DeserializeEntries(entries, CDModel.DbRecordEntryType.Employment),
            adminEntries: DeserializeEntries(entries, CDModel.DbRecordEntryType.Admin));
    }

    private static CDModel.CharacterRecordEntry ConvertEntry(
        PlayerProvidedCharacterRecords.RecordEntry entry,
        CDModel.DbRecordEntryType type)
    {
        entry.EnsureValid();
        return new CDModel.CharacterRecordEntry
        {
            Title = entry.Title,
            Involved = entry.Involved,
            Description = entry.Description,
            Type = type,
        };
    }

    public static List<CDModel.CharacterRecordEntry> GetEntries(PlayerProvidedCharacterRecords records)
    {
        // Flatten the per-category lists into the database representation while reusing the validation logic above.
        return records.MedicalEntries.Select(medical => ConvertEntry(medical, CDModel.DbRecordEntryType.Medical))
            .Concat(records.SecurityEntries.Select(security => ConvertEntry(security, CDModel.DbRecordEntryType.Security)))
            .Concat(records.EmploymentEntries.Select(employment => ConvertEntry(employment, CDModel.DbRecordEntryType.Employment)))
            .Concat(records.AdminEntries.Select(admin => ConvertEntry(admin, CDModel.DbRecordEntryType.Admin)))
            .ToList();
    }
}
