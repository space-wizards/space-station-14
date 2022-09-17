using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects.Components.Localization;

namespace Content.Shared.Localizations
{
    public sealed class ContentLocalizationManager
    {
        [Dependency] private readonly ILocalizationManager _loc = default!;
        [Dependency] private readonly IEntityManager _ent = default!;

        // If you want to change your codebase's language, do it here.
        private const string Culture = "en-US";

        /// <summary>
        /// Custom format strings used for parsing and displaying minutes:seconds timespans.
        /// </summary>
        public static readonly string[] TimeSpanMinutesFormats = new[]
        {
            @"m\:ss",
            @"mm\:ss",
            @"%m",
            @"mm"
        };

        public void Initialize()
        {
            var culture = new CultureInfo(Culture);

            _loc.LoadCulture(culture);
            _loc.AddFunction(culture, "PRESSURE", FormatPressure);
            _loc.AddFunction(culture, "POWERWATTS", FormatPowerWatts);
            _loc.AddFunction(culture, "POWERJOULES", FormatPowerJoules);
            _loc.AddFunction(culture, "UNITS", FormatUnits);
            _loc.AddFunction(culture, "TOSTRING", args => FormatToString(culture, args));
            _loc.AddFunction(culture, "LOC", FormatLoc);

            // Grammatical gender / pronouns
            _loc.AddFunction(culture, "GENDER", FuncGender);
            _loc.AddFunction(culture, "SUBJECT", FuncSubject);
            _loc.AddFunction(culture, "OBJECT", FuncObject);
            _loc.AddFunction(culture, "POSS-ADJ", FuncPossAdj);
            _loc.AddFunction(culture, "POSS-PRONOUN", FuncPossPronoun);
            _loc.AddFunction(culture, "REFLEXIVE", FuncReflexive);

            // Conjugation
            _loc.AddFunction(culture, "CONJUGATE-BE", FuncConjugateBe);
            _loc.AddFunction(culture, "CONJUGATE-HAVE", FuncConjugateHave);

            // Proper nouns
            _loc.AddFunction(culture, "PROPER", FuncProper);
            _loc.AddFunction(culture, "THE", FuncThe);

            // Misc
            _loc.AddFunction(culture, "CAPITALIZE", FuncCapitalize);

            // These functions are so english-specific that they really have no place in other cultures.
            if (Culture == "en-US")
            {
                _loc.AddFunction(culture, "INDEFINITE", FuncIndefiniteEnglish);
            }
        }

        private static ILocValue FormatLoc(LocArgs args)
        {
            var id = ((LocValueString)args.Args[0]).Value;

            return new LocValueString(Loc.GetString(id));
        }

        private static ILocValue FormatToString(CultureInfo culture, LocArgs args)
        {
            var arg = args.Args[0];
            var fmt = ((LocValueString) args.Args[1]).Value;

            var obj = arg.Value;
            if (obj is IFormattable formattable)
                return new LocValueString(formattable.ToString(fmt, culture));

            return new LocValueString(obj?.ToString() ?? "");
        }

        private static ILocValue FormatUnitsGeneric(LocArgs args, string mode)
        {
            const int maxPlaces = 5; // Matches amount in _lib.ftl
            var pressure = ((LocValueNumber) args.Args[0]).Value;

            var places = 0;
            while (pressure > 1000 && places < maxPlaces)
            {
                pressure /= 1000;
                places += 1;
            }

            return new LocValueString(Loc.GetString(mode, ("divided", pressure), ("places", places)));
        }

        private static ILocValue FormatPressure(LocArgs args)
        {
            return FormatUnitsGeneric(args, "zzzz-fmt-pressure");
        }

        private static ILocValue FormatPowerWatts(LocArgs args)
        {
            return FormatUnitsGeneric(args, "zzzz-fmt-power-watts");
        }

        private static ILocValue FormatPowerJoules(LocArgs args)
        {
            return FormatUnitsGeneric(args, "zzzz-fmt-power-joules");
        }

        private static ILocValue FormatUnits(LocArgs args)
        {
            if (!Units.Types.TryGetValue(((LocValueString) args.Args[0]).Value, out var ut))
                throw new ArgumentException($"Unknown unit type {((LocValueString) args.Args[0]).Value}");

            var fmtstr = ((LocValueString) args.Args[1]).Value;

            double max = Double.NegativeInfinity;
            var iargs = new double[args.Args.Count - 1];
            for (var i = 2; i < args.Args.Count; i++)
            {
                var n = ((LocValueNumber) args.Args[i]).Value;
                if (n > max)
                    max = n;

                iargs[i - 2] = n;
            }

            if (!ut!.TryGetUnit(max, out var mu))
                throw new ArgumentException("Unit out of range for type");

            var fargs = new object[iargs.Length];

            for (var i = 0; i < iargs.Length; i++)
                fargs[i] = iargs[i] * mu.Factor;

            fargs[^1] = Loc.GetString($"units-{mu.Unit.ToLower()}");

            // Before anyone complains about "{"+"${...}", at least it's better than MS's approach...
            // https://docs.microsoft.com/en-us/dotnet/standard/base-types/composite-formatting#escaping-braces
            //
            // Note that the closing brace isn't replaced so that format specifiers can be applied.
            var res = String.Format(
                    fmtstr.Replace("{UNIT", "{" + $"{fargs.Length - 1}"),
                    fargs
            );

            return new LocValueString(res);
        }


        /// <summary>
        /// Returns the name of the entity passed in, prepended with "the" if it is not a proper noun.
        /// </summary>
        private ILocValue FuncThe(LocArgs args)
        {
            return new LocValueString(Loc.GetString("zzzz-the", ("ent", args.Args[0])));
        }

        /// <summary>
        /// Returns the string passed in, with the first letter capitalized.
        /// </summary>
        private ILocValue FuncCapitalize(LocArgs args)
        {
            var input = args.Args[0].Format(new LocContext());
            if (!String.IsNullOrEmpty(input))
                return new LocValueString(input[0].ToString().ToUpper() + input.Substring(1));
            else return new LocValueString("");
        }

        private static readonly string[] IndefExceptions = { "euler", "heir", "honest" };
        private static readonly char[] IndefCharList = { 'a', 'e', 'd', 'h', 'i', 'l', 'm', 'n', 'o', 'r', 's', 'x' };
        private static readonly Regex[] IndefRegexes =
        {
            new ("^e[uw]"),
            new ("^onc?e\b"),
            new ("^uni([^nmd]|mo)"),
            new ("^u[bcfhjkqrst][aeiou]")
        };

        private static readonly Regex IndefRegexFjo =
            new("(?!FJO|[HLMNS]Y.|RY[EO]|SQU|(F[LR]?|[HL]|MN?|N|RH?|S[CHKLMNPTVW]?|X(YL)?)[AEIOU])[FHLMNRSX][A-Z]");

        private static readonly Regex IndefRegexU = new("^U[NK][AIEO]");

        private static readonly Regex IndefRegexY =
            new("^y(b[lor]|cl[ea]|fere|gg|p[ios]|rou|tt)");

        private static readonly char[] IndefVowels = { 'a', 'e', 'i', 'o', 'u' };

        private ILocValue FuncIndefiniteEnglish(LocArgs args)
        {
            ILocValue val = args.Args[0];
            if (val.Value == null)
                return new LocValueString("an");

            string? word;
            string? input;
            if (val.Value is EntityUid entity)
            {
                if (_loc.TryGetEntityLocAttrib(entity, "indefinite", out var indef))
                    return new LocValueString(indef);

                input = _ent.GetComponent<MetaDataComponent>(entity).EntityName;
            }
            else
            {
                input = val.Format(new LocContext());
            }

            if (String.IsNullOrEmpty(input))
                return new LocValueString("");

            var a = new LocValueString("a");
            var an = new LocValueString("an");

            var m = Regex.Match(input, @"\w+");
            if (m.Success)
            {
                word = m.Groups[0].Value;
            }
            else
            {
                return an;
            }

            var wordi = word.ToLower();
            if (IndefExceptions.Any(anword => wordi.StartsWith(anword)))
            {
                return an;
            }

            if (wordi.StartsWith("hour") && !wordi.StartsWith("houri"))
                return an;

            if (wordi.Length == 1)
            {
                return wordi.IndexOfAny(IndefCharList) == 0 ? an : a;
            }

            if (IndefRegexFjo.Match(word)
                .Success)
            {
                return an;
            }

            foreach (var regex in IndefRegexes)
            {
                if (regex.IsMatch(wordi))
                    return a;
            }

            if (IndefRegexU.IsMatch(word))
            {
                return a;
            }

            if (word == word.ToUpper())
            {
                return wordi.IndexOfAny(IndefCharList) == 0 ? an : a;
            }

            if (wordi.IndexOfAny(IndefVowels) == 0)
            {
                return an;
            }

            return IndefRegexY.IsMatch(wordi) ? an : a;
        }

        /// <summary>
        /// Returns the gender of the entity passed in; either Male, Female, Neuter or Epicene.
        /// </summary>
        private ILocValue FuncGender(LocArgs args)
        {
            if (args.Args.Count < 1) return new LocValueString(nameof(Gender.Neuter));

            ILocValue entity0 = args.Args[0];
            if (entity0.Value != null)
            {
                EntityUid entity = (EntityUid)entity0.Value;

                if (_ent.TryGetComponent<GrammarComponent?>(entity, out var grammar) && grammar.Gender.HasValue)
                {
                    return new LocValueString(grammar.Gender.Value.ToString().ToLowerInvariant());
                }

                if (_loc.TryGetEntityLocAttrib(entity, "gender", out var gender))
                {
                    return new LocValueString(gender);
                }
            }

            return new LocValueString(nameof(Gender.Neuter));
        }

        /// <summary>
        /// Returns the respective subject pronoun (he, she, they, it) for the entity's gender.
        /// </summary>
        private ILocValue FuncSubject(LocArgs args)
        {
            return new LocValueString(Loc.GetString("zzzz-subject-pronoun", ("ent", args.Args[0])));
        }

        /// <summary>
        /// Returns the respective object pronoun (him, her, them, it) for the entity's gender.
        /// </summary>
        private ILocValue FuncObject(LocArgs args)
        {
            return new LocValueString(Loc.GetString("zzzz-object-pronoun", ("ent", args.Args[0])));
        }

        /// <summary>
        /// Returns the respective possessive adjective (his, her, their, its) for the entity's gender.
        /// </summary>
        private ILocValue FuncPossAdj(LocArgs args)
        {
            return new LocValueString(Loc.GetString("zzzz-possessive-adjective", ("ent", args.Args[0])));
        }

        /// <summary>
        /// Returns the respective possessive pronoun (his, hers, theirs, its) for the entity's gender.
        /// </summary>
        private ILocValue FuncPossPronoun(LocArgs args)
        {
            return new LocValueString(Loc.GetString("zzzz-possessive-pronoun", ("ent", args.Args[0])));
        }

        /// <summary>
        /// Returns the respective reflexive pronoun (himself, herself, themselves, itself) for the entity's gender.
        /// </summary>
        private ILocValue FuncReflexive(LocArgs args)
        {
            return new LocValueString(Loc.GetString("zzzz-reflexive-pronoun", ("ent", args.Args[0])));
        }

        /// <summary>
        /// Returns the respective conjugated form of "to be" (is for male/female/neuter, are for epicene) for the entity's gender.
        /// </summary>
        private ILocValue FuncConjugateBe(LocArgs args)
        {
            return new LocValueString(Loc.GetString("zzzz-conjugate-be", ("ent", args.Args[0])));
        }

        /// <summary>
        /// Returns the respective conjugated form of "to have" (has for male/female/neuter, have for epicene) for the entity's gender.
        /// </summary>
        private ILocValue FuncConjugateHave(LocArgs args)
        {
            return new LocValueString(Loc.GetString("zzzz-conjugate-have", ("ent", args.Args[0])));
        }

        /// <summary>
        /// Returns whether the passed in entity's name is proper or not.
        /// </summary>
        private ILocValue FuncProper(LocArgs args)
        {
            if (args.Args.Count < 1) return new LocValueString("false");

            ILocValue entity0 = args.Args[0];
            if (entity0.Value != null)
            {
                EntityUid entity = (EntityUid)entity0.Value;

                if (_ent.TryGetComponent<GrammarComponent?>(entity, out var grammar) && grammar.ProperNoun.HasValue)
                {
                    return new LocValueString(grammar.ProperNoun.Value.ToString().ToLowerInvariant());
                }

                if (_loc.TryGetEntityLocAttrib(entity, "proper", out var proper))
                {
                    return new LocValueString(proper);
                }
            }

            return new LocValueString("false");
        }
    }
}
