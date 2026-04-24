using Content.Shared.Damage;
using Robust.Shared.Utility;

namespace Content.Shared._Offbrand.Wounds;

public static class DamageSpecifierExtensions
{
    extension(DamageSpecifier specifier)
    {
        public DamageSpecifier Heal(DamageSpecifier incoming)
        {
            var remainder = new DamageSpecifier(incoming);

            foreach (var (type, value) in remainder.DamageDict)
            {
                DebugTools.Assert(value <= 0);

                if (!specifier.DamageDict.TryGetValue(type, out var existing))
                    continue;

                var newValue = existing + value;
                if (newValue <= 0)
                {
                    remainder.DamageDict[type] = newValue;
                    newValue = 0;
                }
                else
                {
                    remainder.DamageDict[type] = 0;
                }

                specifier.DamageDict[type] = newValue;
            }

            remainder.TrimZeros();
            return remainder;
        }
    }
}
