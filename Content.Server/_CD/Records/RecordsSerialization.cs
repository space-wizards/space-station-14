using Content.Server.Database;
using Content.Shared._CD.Records;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;

namespace Content.Server._CD.Records;

public static class RecordsSerialization
{
    private static int DeserializeInt(JsonElement e, string key, int def)
    {
        if (e.TryGetProperty(key, out var prop) && prop.TryGetInt32(out var v))
        {
            return v;
        }

        return def;
    }

    private static bool DeserializeBool(JsonElement e, string key, bool def)
    {
        if (!e.TryGetProperty(key, out var v))
            return def;

        return v.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => def,
        };
    }

    [return: NotNullIfNotNull(nameof(def))]
    private static string? DeserializeString(JsonElement e, string key, string? def)
    {
        if (!e.TryGetProperty(key, out var v))
            return def;

        if (v.ValueKind == JsonValueKind.String)
            return v.GetString() ?? def;

        return def;
    }

    private static List<PlayerProvidedCharacterRecords.RecordEntry> DeserializeEntries(List<CDModel.CharacterRecordEntry> entries, CDModel.DbRecordEntryType ty)
    {
        return entries.Where(e => e.Type == ty)
            .OrderBy(e => e.Id) // attempt at fixing the record order changing bug.
            .Select(e => new PlayerProvidedCharacterRecords.RecordEntry(e.Title, e.Involved, e.Description))
            .ToList();
    }

    /// <summary>
    /// We need to manually deserialize CharacterRecords because the easy JSON deserializer does not
    /// do exactly what we want. More specifically, we need to more robustly handle missing and extra fields
    /// <br />
    /// <br />
    /// Missing fields are filled in with their default value, extra fields are simply ignored
    /// </summary>
    public static PlayerProvidedCharacterRecords Deserialize(JsonDocument json, List<CDModel.CharacterRecordEntry> entries)
    {
        var e = json.RootElement;
        var def = PlayerProvidedCharacterRecords.DefaultRecords();
        return new PlayerProvidedCharacterRecords(
            height: DeserializeInt(e, nameof(def.Height), def.Height),
            weight: DeserializeInt(e, nameof(def.Weight), def.Weight),
            year: DeserializeInt(e, nameof(def.Year), def.Year),
            emergencyContactName: DeserializeString(e, nameof(def.EmergencyContactName), def.EmergencyContactName),
            hasWorkAuthorization: DeserializeBool(e, nameof(def.HasWorkAuthorization), def.HasWorkAuthorization),
            identifyingFeatures: DeserializeString(e, nameof(def.IdentifyingFeatures), def.IdentifyingFeatures),
            allergies: DeserializeString(e, nameof(def.Allergies), def.Allergies),
            drugAllergies: DeserializeString(e, nameof(def.DrugAllergies), def.DrugAllergies),
            postmortemInstructions: DeserializeString(e, nameof(def.PostmortemInstructions), def.PostmortemInstructions),
            medicalEntries: DeserializeEntries(entries, CDModel.DbRecordEntryType.Medical),
            securityEntries: DeserializeEntries(entries, CDModel.DbRecordEntryType.Security),
            employmentEntries: DeserializeEntries(entries, CDModel.DbRecordEntryType.Employment));
    }

    private static CDModel.CharacterRecordEntry ConvertEntry(PlayerProvidedCharacterRecords.RecordEntry entry, CDModel.DbRecordEntryType type)
    {
        entry.EnsureValid();
        return new CDModel.CharacterRecordEntry()
            { Title = entry.Title, Involved = entry.Involved, Description = entry.Description, Type = type };
    }

    public static List<CDModel.CharacterRecordEntry> GetEntries(PlayerProvidedCharacterRecords records)
    {
        return records.MedicalEntries.Select(medical => ConvertEntry(medical, CDModel.DbRecordEntryType.Medical))
            .Concat(records.SecurityEntries.Select(security => ConvertEntry(security, CDModel.DbRecordEntryType.Security)))
            .Concat(records.EmploymentEntries.Select(employment => ConvertEntry(employment, CDModel.DbRecordEntryType.Employment)))
            .ToList();
    }
}
