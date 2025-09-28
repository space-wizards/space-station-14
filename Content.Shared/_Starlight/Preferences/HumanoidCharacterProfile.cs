using System.Text.RegularExpressions;
using Content.Shared.CCVar;

// ReSharper disable CheckNamespace

namespace Content.Shared.Preferences;

public sealed partial class HumanoidCharacterProfile
{
    private static readonly Regex RestrictedCustomSpecieNameRegex = new(@"[^A-Za-z0-9 '\-,]|\B\s+|\s+\B"); //Starlight

    [DataField] public string SiliconVoice { get; set; } = "";

    [DataField] public string PersonalityDescription { get; set; } = string.Empty;

    [DataField] public string PersonalNotes { get; set; } = string.Empty;

    [DataField] public string OOCNotes { get; set; } = string.Empty;

    [DataField] public string Secrets { get; set; } = string.Empty;

    [DataField] public string ExploitableInfo { get; set; } = string.Empty;

    [DataField] public string CustomSpecieName { get; set; } = "";

    [DataField] public List<string> Cybernetics = [];

    [DataField] public string PhysicalDescription { get; set; } = string.Empty;

    /// <summary>
    /// Detailed text that can appear for the character if <see cref="CCVars.FlavorText"/> is enabled.
    /// </summary>
    [DataField]
    [Obsolete("Use PhysicalDescription instead!")]
    public string FlavorText
    {
        get => PhysicalDescription;
        set => PhysicalDescription = value;
    }

    public HumanoidCharacterProfile WithPhysicalDesc(string physicalDesc)
    {
        return new(this) { PhysicalDescription = physicalDesc };
    }

    public HumanoidCharacterProfile WithPersonalityDesc(string personalityDesc)
    {
        return new(this) { PersonalityDescription = personalityDesc };
    }

    public HumanoidCharacterProfile WithSecrets(string secrets)
    {
        return new(this) { Secrets = secrets };
    }

    public HumanoidCharacterProfile WithPersonalNotes(string personalNotes)
    {
        return new(this) { PersonalNotes = personalNotes };
    }

    public HumanoidCharacterProfile WithExploitable(string exploitable)
    {
        return new(this) { ExploitableInfo = exploitable };
    }

    public HumanoidCharacterProfile WithOOCNotes(string oocNotes)
    {
        return new(this) { OOCNotes = oocNotes };
    }

    public HumanoidCharacterProfile WithSiliconVoice(string id)
    {
        return new(this) { SiliconVoice = id };
    }

    public HumanoidCharacterProfile WithCustomSpecieName(string customspeciename)
    {
        return new(this) { CustomSpecieName = customspeciename };
    }

    public HumanoidCharacterProfile WithCybernetics(List<string> cybernetics)
    {
        return new(this) { Cybernetics = cybernetics, };
    }
}