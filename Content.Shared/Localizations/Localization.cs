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

        public static void Init()
        {
            var loc = IoCManager.Resolve<ILocalizationManager>();
            var res = IoCManager.Resolve<IResourceManager>();

            var culture = new CultureInfo(Culture);

            loc.LoadCulture(culture);
            loc.AddFunction(culture, "PRESSURE", FormatPressure);
            loc.AddFunction(culture, "TOSTRING", args => FormatToString(culture, args));
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

        private static ILocValue FormatPressure(LocArgs args)
        {
            const int maxPlaces = 5; // Matches amount in _lib.ftl
            var pressure = ((LocValueNumber) args.Args[0]).Value;

            var places = 0;
            while (pressure > 1000 && places < maxPlaces)
            {
                pressure /= 1000;
                places += 1;
            }

            return new LocValueString(Loc.GetString("zzzz-fmt-pressure", ("divided", pressure), ("places", places)));
        }
    }
}
