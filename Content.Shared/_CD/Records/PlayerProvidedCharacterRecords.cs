using System.Linq;
using System.Text.Json.Serialization;
using Robust.Shared.Serialization;

namespace Content.Shared._CD.Records;

/// <summary>
/// Contains Cosmatic Drift records that can be changed in the character editor. This is stored on the character's profile.
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class PlayerProvidedCharacterRecords
{
    public const int TextMedLen = 64;
    public const int TextVeryLargeLen = 4096;

    /* Basic info */

    // Additional data is fetched from the Profile

    // All
    [DataField]
    public int Height { get; private set; }
    public const int MaxHeight = 800;

    [DataField]
    public int Weight { get; private set; }
    public const int MaxWeight = 300;

    [DataField]
    public string EmergencyContactName { get; private set; }

    // Employment
    [DataField]
    public int Year { get; private set; }
    public const int MaxYear = 150;

    [DataField]
    public bool HasWorkAuthorization { get; private set; }

    // Security
    [DataField]
    public string IdentifyingFeatures { get; private set; }

    // Medical
    [DataField]
    public string Allergies { get; private set; }
    [DataField]
    public string DrugAllergies { get; private set; }
    [DataField]
    public string PostmortemInstructions { get; private set; }
    // history, prescriptions, etc. would be a record below

    // "incidents"
    [DataField, JsonIgnore]
    public List<RecordEntry> MedicalEntries { get; private set; }
    [DataField, JsonIgnore]
    public List<RecordEntry> SecurityEntries { get; private set; }
    [DataField, JsonIgnore]
    public List<RecordEntry> EmploymentEntries { get; private set; }

    [DataDefinition]
    [Serializable, NetSerializable]
    public sealed partial class RecordEntry
    {
        [DataField]
        public string Title { get; private set; }
        // players involved, can be left blank (or with a generic "CentCom" etc.) for backstory related issues
        [DataField]
        public string Involved { get; private set; }
        // Longer description of events.
        [DataField]
        public string Description { get; private set; }

        public RecordEntry(string title, string involved, string desc)
        {
            Title = title;
            Involved = involved;
            Description = desc;
        }

        public RecordEntry(RecordEntry other)
        : this(other.Title, other.Involved, other.Description)
        {
        }

        public bool MemberwiseEquals(RecordEntry other)
        {
            return Title == other.Title && Involved == other.Involved && Description == other.Description;
        }

        public void EnsureValid()
        {
            Title = ClampString(Title, TextMedLen);
            Involved = ClampString(Involved, TextMedLen);
            Description = ClampString(Description, TextVeryLargeLen);
        }
    }

    public PlayerProvidedCharacterRecords(
        bool hasWorkAuthorization,
        int height, int weight,
        int year,
        string emergencyContactName,
        string identifyingFeatures,
        string allergies, string drugAllergies,
        string postmortemInstructions,
        List<RecordEntry> medicalEntries, List<RecordEntry> securityEntries, List<RecordEntry> employmentEntries)
    {
        HasWorkAuthorization = hasWorkAuthorization;
        Height = height;
        Weight = weight;
        Year = year;
        EmergencyContactName = emergencyContactName;
        IdentifyingFeatures = identifyingFeatures;
        Allergies = allergies;
        DrugAllergies = drugAllergies;
        PostmortemInstructions = postmortemInstructions;
        MedicalEntries = medicalEntries;
        SecurityEntries = securityEntries;
        EmploymentEntries = employmentEntries;
    }

    public PlayerProvidedCharacterRecords(PlayerProvidedCharacterRecords other)
    {
        Height = other.Height;
        Weight = other.Weight;
        Year = other.Year;
        EmergencyContactName = other.EmergencyContactName;
        HasWorkAuthorization = other.HasWorkAuthorization;
        IdentifyingFeatures = other.IdentifyingFeatures;
        Allergies = other.Allergies;
        DrugAllergies = other.DrugAllergies;
        PostmortemInstructions = other.PostmortemInstructions;
        MedicalEntries = other.MedicalEntries.Select(x => new RecordEntry(x)).ToList();
        SecurityEntries = other.SecurityEntries.Select(x => new RecordEntry(x)).ToList();
        EmploymentEntries = other.EmploymentEntries.Select(x => new RecordEntry(x)).ToList();
    }

    public static PlayerProvidedCharacterRecords DefaultRecords()
    {
        return new PlayerProvidedCharacterRecords(
            hasWorkAuthorization: true,
            height: 170, weight: 70,
            emergencyContactName: "",
            identifyingFeatures: "",
            year: 0,
            allergies: "None",
            drugAllergies: "None",
            postmortemInstructions: "Return home",
            medicalEntries: new List<RecordEntry>(),
            securityEntries: new List<RecordEntry>(),
            employmentEntries: new List<RecordEntry>()
        );
    }

    public bool MemberwiseEquals(PlayerProvidedCharacterRecords other)
    {
        // This is ugly but is only used for integration tests.
        var test = Height == other.Height
                   && Weight == other.Weight
                   && Year == other.Year
                   && EmergencyContactName == other.EmergencyContactName
                   && HasWorkAuthorization == other.HasWorkAuthorization
                   && IdentifyingFeatures == other.IdentifyingFeatures
                   && Allergies == other.Allergies
                   && DrugAllergies == other.DrugAllergies
                   && PostmortemInstructions == other.PostmortemInstructions;
        if (!test)
            return false;
        if (MedicalEntries.Count != other.MedicalEntries.Count)
            return false;
        if (SecurityEntries.Count != other.SecurityEntries.Count)
            return false;
        if (EmploymentEntries.Count != other.EmploymentEntries.Count)
            return false;
        if (MedicalEntries.Where((t, i) => !t.MemberwiseEquals(other.MedicalEntries[i])).Any())
        {
            return false;
        }
        if (SecurityEntries.Where((t, i) => !t.MemberwiseEquals(other.SecurityEntries[i])).Any())
        {
            return false;
        }
        if (EmploymentEntries.Where((t, i) => !t.MemberwiseEquals(other.EmploymentEntries[i])).Any())
        {
            return false;
        }

        return true;
    }

    private static string ClampString(string str, int maxLen)
    {
        if (str.Length > maxLen)
        {
            return str[..maxLen];
        }
        return str;
    }

    private static void EnsureValidEntries(List<RecordEntry> entries)
    {
        foreach (var entry in entries)
        {
            entry.EnsureValid();
        }
    }

    /// <summary>
    /// Clamp invalid entries to valid values
    /// </summary>
    public void EnsureValid()
    {
        Height = Math.Clamp(Height, 0, MaxHeight);
        Weight = Math.Clamp(Weight, 0, MaxWeight);
        Year = Math.Clamp(Year, 0, MaxYear);
        EmergencyContactName =
            ClampString(EmergencyContactName, TextMedLen);
        IdentifyingFeatures = ClampString(IdentifyingFeatures, TextMedLen);
        Allergies = ClampString(Allergies, TextMedLen);
        DrugAllergies = ClampString(DrugAllergies, TextMedLen);
        PostmortemInstructions = ClampString(PostmortemInstructions, TextMedLen);

        EnsureValidEntries(EmploymentEntries);
        EnsureValidEntries(MedicalEntries);
        EnsureValidEntries(SecurityEntries);
    }
    public PlayerProvidedCharacterRecords WithHeight(int height)
    {
        return new(this) { Height = height };
    }
    public PlayerProvidedCharacterRecords WithWeight(int weight)
    {
        return new(this) { Weight = weight };
    }
    public PlayerProvidedCharacterRecords WithYear(int year)
    {
        return new(this) { Year = year };
    }
    public PlayerProvidedCharacterRecords WithWorkAuth(bool auth)
    {
        return new(this) { HasWorkAuthorization = auth };
    }
    public PlayerProvidedCharacterRecords WithContactName(string name)
    {
        return new(this) { EmergencyContactName = name};
    }
    public PlayerProvidedCharacterRecords WithIdentifyingFeatures(string feat)
    {
        return new(this) { IdentifyingFeatures = feat};
    }
    public PlayerProvidedCharacterRecords WithAllergies(string s)
    {
        return new(this) { Allergies = s };
    }
    public PlayerProvidedCharacterRecords WithDrugAllergies(string s)
    {
        return new(this) { DrugAllergies = s };
    }
    public PlayerProvidedCharacterRecords WithPostmortemInstructions(string s)
    {
        return new(this) { PostmortemInstructions = s};
    }
    public PlayerProvidedCharacterRecords WithEmploymentEntries(List<RecordEntry> entries)
    {
        return new(this) { EmploymentEntries = entries};
    }
    public PlayerProvidedCharacterRecords WithMedicalEntries(List<RecordEntry> entries)
    {
        return new(this) { MedicalEntries = entries};
    }
    public PlayerProvidedCharacterRecords WithSecurityEntries(List<RecordEntry> entries)
    {
        return new(this) { SecurityEntries = entries};
    }
}

public enum CharacterRecordType : byte
{
    Employment, Medical, Security
}
