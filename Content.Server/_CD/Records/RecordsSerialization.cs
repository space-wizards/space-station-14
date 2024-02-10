using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Content.Shared._CD.Records;

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
        if (e.TryGetProperty(key, out var v))
        {
            if (v.ValueKind == JsonValueKind.True)
                return true;
            if (v.ValueKind == JsonValueKind.False)
                return false;
        }

        return def;
    }

    [return: NotNullIfNotNull(nameof(def))]
    private static string? DeserializeString(JsonElement e, string key, string? def)
    {
        if (e.TryGetProperty(key, out var v))
        {
            if (v.ValueKind == JsonValueKind.String)
                return v.GetString() ?? def;
        }

        return def;
    }

    private static List<CharacterRecords.RecordEntry> DeserializeEntries(JsonElement e, string key)
    {
        if (e.TryGetProperty(key, out var arrv))
        {
            if (arrv.ValueKind != JsonValueKind.Array)
                return new List<CharacterRecords.RecordEntry>();

            var res = new List<CharacterRecords.RecordEntry>();
            for (var i = 0; i < arrv.GetArrayLength(); ++i)
            {
                var record = arrv[i];
                var title = DeserializeString(record, nameof(CharacterRecords.RecordEntry.Title), null);
                var involved = DeserializeString(record, nameof(CharacterRecords.RecordEntry.Involved), null);
                var desc = DeserializeString(record, nameof(CharacterRecords.RecordEntry.Description), null);
                if (title == null || involved == null || desc == null)
                    return new List<CharacterRecords.RecordEntry>();
                res.Add(new CharacterRecords.RecordEntry(title, involved, desc));
            }

            return res;
        }

        return new List<CharacterRecords.RecordEntry>();
    }

    /// <summary>
    /// We need to manually deserialize CharacterRecords because the easy JSON deserializer does not
    /// do exactly what we want. More specifically, we need to more robustly handle missing and extra fields
    /// <br />
    /// <br />
    /// Missing fields are filled in with their default value, extra fields are simply ignored
    /// </summary>
    public static CharacterRecords DeserializeJson(JsonDocument json)
    {
        var e = json.RootElement;
        var def = CharacterRecords.DefaultRecords();
        return new CharacterRecords(
            height: DeserializeInt(e, nameof(def.Height), def.Height),
            weight: DeserializeInt(e, nameof(def.Weight), def.Weight),
            emergencyContactName: DeserializeString(e, nameof(def.EmergencyContactName), def.EmergencyContactName),
            hasWorkAuthorization: DeserializeBool(e, nameof(def.HasWorkAuthorization), def.HasWorkAuthorization),
            identifyingFeatures: DeserializeString(e, nameof(def.IdentifyingFeatures), def.IdentifyingFeatures),
            allergies: DeserializeString(e, nameof(def.Allergies), def.Allergies), drugAllergies: DeserializeString(e, nameof(def.DrugAllergies), def.DrugAllergies),
            postmortemInstructions: DeserializeString(e, nameof(def.PostmortemInstructions), def.PostmortemInstructions),
            medicalEntries: DeserializeEntries(e, nameof(def.MedicalEntries)),
            securityEntries: DeserializeEntries(e, nameof(def.SecurityEntries)),
            employmentEntries: DeserializeEntries(e, nameof(def.EmploymentEntries)));
    }
}
