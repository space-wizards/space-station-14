namespace Content.Server.Silicons.Laws;

public sealed partial class IonLawLocalizationSystem : EntitySystem
{
    [Dependency] private IonLawSystem _ionLaw = default!;

    public override void Initialize()
    {
        base.Initialize();

        var culture = Loc.DefaultCulture;

        if (culture == null)
        {
            Log.Error("Culture was null when trying to generate Ion Law");
            return;
        }

        Loc.AddFunction(culture, "ION-NUMBER-BASE", _ => GetIonLawValue("ION-NUMBER-BASE"));
        Loc.AddFunction(culture, "ION-NUMBER-MOD", _ => GetIonLawValue("ION-NUMBER-MOD"));
        Loc.AddFunction(culture, "ION-ADJECTIVE", _ => GetIonLawValue("ION-ADJECTIVE"));
        Loc.AddFunction(culture, "ION-SUBJECT", _ => GetIonLawValue("ION-SUBJECT"));
        Loc.AddFunction(culture, "ION-WHO", _ => GetIonLawValue("ION-WHO"));
        Loc.AddFunction(culture, "ION-MUST", _ => GetIonLawValue("ION-MUST"));
        Loc.AddFunction(culture, "ION-THING", _ => GetIonLawValue("ION-THING"));
        Loc.AddFunction(culture, "ION-JOB", _ => GetIonLawValue("ION-JOB"));
        Loc.AddFunction(culture, "ION-WHO-GENERAL", _ => GetIonLawValue("ION-WHO-GENERAL"));
        Loc.AddFunction(culture, "ION-PLURAL", _ => GetIonLawValue("ION-PLURAL"));
        Loc.AddFunction(culture, "ION-REQUIRE", _ => GetIonLawValue("ION-REQUIRE"));
        Loc.AddFunction(culture, "ION-SEVERITY", _ => GetIonLawValue("ION-SEVERITY"));
        Loc.AddFunction(culture, "ION-ALLERGY", _ => GetIonLawValue("ION-ALLERGY"));
        Loc.AddFunction(culture, "ION-FEELING", _ => GetIonLawValue("ION-FEELING"));
        Loc.AddFunction(culture, "ION-CONCEPT", _ => GetIonLawValue("ION-CONCEPT"));
        Loc.AddFunction(culture, "ION-FOOD", _ => GetIonLawValue("ION-FOOD"));
        Loc.AddFunction(culture, "ION-DRINK", _ => GetIonLawValue("ION-DRINK"));
        Loc.AddFunction(culture, "ION-CHANGE", _ => GetIonLawValue("ION-CHANGE"));
        Loc.AddFunction(culture, "ION-WHO-RANDOM", _ => GetIonLawValue("ION-WHO-RANDOM"));
        Loc.AddFunction(culture, "ION-AREA", _ => GetIonLawValue("ION-AREA"));
        Loc.AddFunction(culture, "ION-PART", _ => GetIonLawValue("ION-PART"));
        Loc.AddFunction(culture, "ION-OBJECT", _ => GetIonLawValue("ION-OBJECT"));
        Loc.AddFunction(culture, "ION-HARM-PROTECT", _ => GetIonLawValue("ION-HARM-PROTECT"));
        Loc.AddFunction(culture, "ION-VERB", _ => GetIonLawValue("ION-VERB"));
    }

    /// <summary>
    /// Returns a localized value for an ion law token.
    /// </summary>
    private ILocValue GetIonLawValue(string s)
    {
        var val = _ionLaw.GetOrGenerateValue(s);
        return new LocValueString(val.ToString() ?? string.Empty);
    }
}
