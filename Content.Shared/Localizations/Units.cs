using System;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Localizations.Units
{
    public static class Units
    {
        public sealed class TypeTable
        {
            public readonly Entry[] E;

            public TypeTable(params Entry[] e) => E = e;

            public sealed class Entry
            {
                public readonly (float? Min, float? Max) Range;
                public readonly float Factor;
                public readonly string Unit;

                public Entry(ValueTuple<float?, float?> range, float factor, string unit)
                {
                    Range = range;
                    Factor = factor;
                    Unit = unit;
                }
            }

            public bool TryGetUnit(float val, [NotNullWhen(true)] out Entry? winner)
            {
                Entry? w = default!;
                foreach (var e in E)
                    if ((e.Range.Min == null || e.Range.Min <= val) && (e.Range.Max == null || val < e.Range.Max))
                        w = e;

                winner = w;
                return w != null;
            }

            public string Format(float val)
            {
                if (TryGetUnit(val, out var w))
                    return (val * w.Factor).ToString() + " " + w.Unit;

                return val.ToString();
            }

            public string Format(float val, string fmt)
            {
                if (TryGetUnit(val, out var w))
                    return (val * w.Factor).ToString(fmt) + " " + w.Unit;

                return val.ToString(fmt);
            }
        }

        // Someone should probably port this to YAML, but it ain't gonna be me.

        public static readonly TypeTable Pressure = new TypeTable
        (
            new TypeTable.Entry(range: (null, 1e-3f), factor: 1e6f, unit: "µPa"),
            new TypeTable.Entry(range: (1e-3f, 1f), factor: 1e3f, unit: "mPa"),
            new TypeTable.Entry(range: (1f, 1000f), factor: 1f, unit: "Pa"),
            new TypeTable.Entry(range: (1000f, 1e6f), factor: 1e-4f, unit: "kPa"),
            new TypeTable.Entry(range: (1e6f, 1e9f), factor: 1e-6f, unit: "MPa"),
            new TypeTable.Entry(range: (1e9f, null), factor: 1e-9f, unit: "GPa")
        );

        public static readonly TypeTable Power = new TypeTable
        (
            new TypeTable.Entry(range: (null, 1e-3f), factor: 1e6f, unit: "µW"),
            new TypeTable.Entry(range: (1e-3f, 1f), factor: 1e3f, unit: "mW"),
            new TypeTable.Entry(range: (1f, 1000f), factor: 1f, unit: "W"),
            new TypeTable.Entry(range: (1000f, 1e6f), factor: 1e-4f, unit: "KW"),
            new TypeTable.Entry(range: (1e6f, 1e9f), factor: 1e-6f, unit: "MW"),
            new TypeTable.Entry(range: (1e9f, null), factor: 1e-9f, unit: "GW")
        );
    }
}
