using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Content.Shared.Localizations
{
    public static class Units
    {
        public sealed class TypeTable
        {
            public readonly Entry[] E;

            public TypeTable(params Entry[] e) => E = e;

            public sealed class Entry
            {
                // Any item within [Min, Max) is considered to be in-range
                // of this Entry.
                public readonly (double? Min, double? Max) Range;

                // Factor is a number that the value will be multiplied by
                // to adjust it in to the proper range.
                public readonly double Factor;

                // Unit is an ID for Fluent. All Units are prefixed with
                // "units-" internally. Usually follows the format $"{unit-abbrev}-{prefix}".
                //
                // Example: "si-g" is actually processed as "units-si-g"
                //
                // As a matter of style, units for values less than 1 (i.e. mW)
                // should have two dashes before their prefix.
                public readonly string Unit;

                public Entry((double?, double?) range, double factor, string unit)
                {
                    Range = range;
                    Factor = factor;
                    Unit = unit;
                }
            }

            public bool TryGetUnit(double val, [NotNullWhen(true)] out Entry? winner)
            {
                Entry? w = default!;
                foreach (var e in E)
                    if ((e.Range.Min == null || e.Range.Min <= val) && (e.Range.Max == null || val < e.Range.Max))
                        w = e;

                winner = w;
                return w != null;
            }

            public string Format(double val)
            {
                if (TryGetUnit(val, out var w))
                    return (val * w.Factor) + " " + Loc.GetString("units-" + w.Unit);

                return val.ToString(CultureInfo.InvariantCulture);
            }

            public string Format(double val, string fmt)
            {
                if (TryGetUnit(val, out var w))
                    return (val * w.Factor).ToString(fmt) + " " + Loc.GetString("units-" + w.Unit);

                return val.ToString(fmt);
            }
        }

        public static readonly TypeTable Generic = new TypeTable
        (
            // Table layout. Fite me.
            new TypeTable.Entry(range: ( null, 1e-24), factor:  1e24, unit: "si--y"),
            new TypeTable.Entry(range: (1e-24, 1e-21), factor:  1e21, unit: "si--z"),
            new TypeTable.Entry(range: (1e-21, 1e-18), factor:  1e18, unit: "si--a"),
            new TypeTable.Entry(range: (1e-18, 1e-15), factor:  1e15, unit: "si--f"),
            new TypeTable.Entry(range: (1e-15, 1e-12), factor:  1e12, unit: "si--p"),
            new TypeTable.Entry(range: (1e-12,  1e-9), factor:   1e9, unit: "si--n"),
            new TypeTable.Entry(range: ( 1e-9,  1e-3), factor:   1e6, unit: "si--u"),
            new TypeTable.Entry(range: ( 1e-3,     1), factor:   1e3, unit: "si--m"),
            new TypeTable.Entry(range: (    1,  1000), factor:     1, unit: "si"),
            new TypeTable.Entry(range: ( 1000,   1e6), factor:  1e-4, unit: "si-k"),
            new TypeTable.Entry(range: (  1e6,   1e9), factor:  1e-6, unit: "si-m"),
            new TypeTable.Entry(range: (  1e9,  1e12), factor:  1e-9, unit: "si-g"),
            new TypeTable.Entry(range: ( 1e12,  1e15), factor: 1e-12, unit: "si-t"),
            new TypeTable.Entry(range: ( 1e15,  1e18), factor: 1e-15, unit: "si-p"),
            new TypeTable.Entry(range: ( 1e18,  1e21), factor: 1e-18, unit: "si-e"),
            new TypeTable.Entry(range: ( 1e21,  1e24), factor: 1e-21, unit: "si-z"),
            new TypeTable.Entry(range: ( 1e24,  null), factor: 1e-24, unit: "si-y")
        );

        // N.B. We use kPa internally, so this is shifted one order of magnitude down.
        public static readonly TypeTable Pressure = new TypeTable
        (
            new TypeTable.Entry(range: (null, 1e-6), factor:  1e9, unit: "u--pascal"),
            new TypeTable.Entry(range: (1e-6, 1e-3), factor:  1e6, unit: "m--pascal"),
            new TypeTable.Entry(range: (1e-3,    1), factor:  1e3, unit: "pascal"),
            new TypeTable.Entry(range: (   1, 1000), factor:    1, unit: "k-pascal"),
            new TypeTable.Entry(range: (1000,  1e6), factor: 1e-4, unit: "m-pascal"),
            new TypeTable.Entry(range: ( 1e6, null), factor: 1e-6, unit: "g-pascal")
        );

        public static readonly TypeTable Power = new TypeTable
        (
            new TypeTable.Entry(range: (null, 1e-3), factor:  1e6, unit: "u--watt"),
            new TypeTable.Entry(range: (1e-3,    1), factor:  1e3, unit: "m--watt"),
            new TypeTable.Entry(range: (   1, 1000), factor:    1, unit: "watt"),
            new TypeTable.Entry(range: (1000,  1e6), factor: 1e-4, unit: "k-watt"),
            new TypeTable.Entry(range: ( 1e6,  1e9), factor: 1e-6, unit: "m-watt"),
            new TypeTable.Entry(range: ( 1e9, null), factor: 1e-9, unit: "g-watt")
        );

        public static readonly TypeTable Energy = new TypeTable
        (
            new TypeTable.Entry(range: ( null, 1e-3), factor:  1e6, unit: "u--joule"),
            new TypeTable.Entry(range: ( 1e-3,    1), factor:  1e3, unit: "m--joule"),
            new TypeTable.Entry(range: (    1, 1000), factor:    1, unit: "joule"),
            new TypeTable.Entry(range: ( 1000,  1e6), factor: 1e-4, unit: "k-joule"),
            new TypeTable.Entry(range: (  1e6,  1e9), factor: 1e-6, unit: "m-joule"),
            new TypeTable.Entry(range: (  1e9, null), factor: 1e-9, unit: "g-joule")
        );

        public static readonly TypeTable Temperature = new TypeTable
        (
            new TypeTable.Entry(range: ( null, 1e-3), factor:  1e6, unit: "u--kelvin"),
            new TypeTable.Entry(range: ( 1e-3,    1), factor:  1e3, unit: "m--kelvin"),
            new TypeTable.Entry(range: (    1,  1e3), factor:    1, unit: "kelvin"),
            new TypeTable.Entry(range: (  1e3,  1e6), factor: 1e-3, unit: "k-kelvin"),
            new TypeTable.Entry(range: (  1e6,  1e9), factor: 1e-6, unit: "m-kelvin"),
            new TypeTable.Entry(range: (  1e9, null), factor: 1e-9, unit: "g-kelvin")
        );

        public readonly static Dictionary<string, TypeTable> Types = new Dictionary<string, TypeTable>
        {
            ["generic"] = Generic,
            ["pressure"] = Pressure,
            ["power"] = Power,
            ["energy"] = Energy,
            ["temperature"] = Temperature,
        };
    }
}
