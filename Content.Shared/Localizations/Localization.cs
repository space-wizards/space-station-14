using System;
using System.Globalization;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Shared.Localizations
{
    public static class Localization
    {
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

        public static void Init()
        {
            var loc = IoCManager.Resolve<ILocalizationManager>();
            var res = IoCManager.Resolve<IResourceManager>();

            var culture = new CultureInfo(Culture);

            loc.LoadCulture(culture);
            loc.AddFunction(culture, "PRESSURE", FormatPressure);
            loc.AddFunction(culture, "POWERWATTS", FormatPowerWatts);
            loc.AddFunction(culture, "POWERJOULES", FormatPowerJoules);
            loc.AddFunction(culture, "TOSTRING", args => FormatToString(culture, args));
            loc.AddFunction(culture, "LOC", FormatLoc);
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
    }
}
