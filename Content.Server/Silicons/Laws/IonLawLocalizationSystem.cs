using System.Globalization;

namespace Content.Server.Silicons.Laws;

public sealed class IonLawLocalizationSystem : EntitySystem
{
    [Dependency] private readonly ILocalizationManager _loc = default!;
    [Dependency] private readonly IonLawSystem _ionLaw = default!;

    public override void Initialize()
    {
        base.Initialize();

        var culture = new CultureInfo("en-US");

        ILocValue Func(string s, LocArgs args)
        {
            var index = args.Args.Count > 1 && args.Args[1] is LocValueNumber n ? (int)n.Value : 0;
            var val = _ionLaw.GetOrGenerateValue(s, index);
            return new LocValueString(val.ToString() ?? string.Empty);
        }

        _loc.AddFunction(culture, "ION-NUMBER-BASE", (args) => Func("ION-NUMBER-BASE", args));
        _loc.AddFunction(culture, "ION-NUMBER-MOD", (args) => Func("ION-NUMBER-MOD", args));
        _loc.AddFunction(culture, "ION-ADJECTIVE", (args) => Func("ION-ADJECTIVE", args));
        _loc.AddFunction(culture, "ION-SUBJECT", (args) => Func("ION-SUBJECT", args));
        _loc.AddFunction(culture, "ION-WHO", (args) => Func("ION-WHO", args));
        _loc.AddFunction(culture, "ION-MUST", (args) => Func("ION-MUST", args));
        _loc.AddFunction(culture, "ION-THING", (args) => Func("ION-THING", args));
        _loc.AddFunction(culture, "ION-JOB", (args) => Func("ION-JOB", args));
        _loc.AddFunction(culture, "ION-WHO-GENERAL", (args) => Func("ION-WHO-GENERAL", args));
        _loc.AddFunction(culture, "ION-PLURAL", (args) => Func("ION-PLURAL", args));
        _loc.AddFunction(culture, "ION-REQUIRE", (args) => Func("ION-REQUIRE", args));
        _loc.AddFunction(culture, "ION-SEVERITY", (args) => Func("ION-SEVERITY", args));
        _loc.AddFunction(culture, "ION-ALLERGY", (args) => Func("ION-ALLERGY", args));
        _loc.AddFunction(culture, "ION-FEELING", (args) => Func("ION-FEELING", args));
        _loc.AddFunction(culture, "ION-CONCEPT", (args) => Func("ION-CONCEPT", args));
        _loc.AddFunction(culture, "ION-FOOD", (args) => Func("ION-FOOD", args));
        _loc.AddFunction(culture, "ION-DRINK", (args) => Func("ION-DRINK", args));
        _loc.AddFunction(culture, "ION-CHANGE", (args) => Func("ION-CHANGE", args));
        _loc.AddFunction(culture, "ION-WHO-RANDOM", (args) => Func("ION-WHO-RANDOM", args));
        _loc.AddFunction(culture, "ION-AREA", (args) => Func("ION-AREA", args));
        _loc.AddFunction(culture, "ION-PART", (args) => Func("ION-PART", args));
        _loc.AddFunction(culture, "ION-OBJECT", (args) => Func("ION-OBJECT", args));
        _loc.AddFunction(culture, "ION-HARM-PROTECT", (args) => Func("ION-HARM-PROTECT", args));
        _loc.AddFunction(culture, "ION-VERB", (args) => Func("ION-VERB", args));
    }
}
