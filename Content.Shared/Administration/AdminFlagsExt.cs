using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Content.Shared.Administration
{
    public static class AdminFlagsExt
    {
        private static readonly Dictionary<string, AdminFlags> NameFlagsMap = new Dictionary<string, AdminFlags>();
        private static readonly string[] FlagsNameMap = new string[32];

        public static readonly AdminFlags Everything;

        public static readonly IReadOnlyList<AdminFlags> AllFlags;

        static AdminFlagsExt()
        {
            var t = typeof(AdminFlags);
            var flags = (AdminFlags[]) Enum.GetValues(t);
            var allFlags = new List<AdminFlags>();

            foreach (var value in flags)
            {
                var name = value.ToString().ToUpper();

                if (BitOperations.PopCount((uint) value) != 1)
                {
                    continue;
                }

                allFlags.Add(value);
                Everything |= value;
                NameFlagsMap.Add(name, value);
                FlagsNameMap[BitOperations.Log2((uint) value)] = name;
            }

            AllFlags = allFlags.ToArray();
        }

        public static AdminFlags NamesToFlags(IEnumerable<string> names)
        {
            var flags = AdminFlags.None;
            foreach (var name in names)
            {
                if (!NameFlagsMap.TryGetValue(name, out var value))
                {
                    throw new ArgumentException($"Invalid admin flag name: {name}");
                }

                flags |= value;
            }

            return flags;
        }

        public static AdminFlags NameToFlag(string name)
        {
            return NameFlagsMap[name];
        }

        public static string[] FlagsToNames(AdminFlags flags)
        {
            var array = new string[BitOperations.PopCount((uint) flags)];
            var highest = BitOperations.LeadingZeroCount((uint) flags);

            var ai = 0;
            for (var i = 0; i < 32 - highest; i++)
            {
                var flagValue = (AdminFlags) (1u << i);
                if ((flags & flagValue) != 0)
                {
                    array[ai++] = FlagsNameMap[i];
                }
            }

            return array;
        }

        public static string PosNegFlagsText(AdminFlags posFlags, AdminFlags negFlags)
        {
            var posFlagNames = FlagsToNames(posFlags).Select(f => (flag: f, fText: $"+{f}"));
            var negFlagNames = FlagsToNames(negFlags).Select(f => (flag: f, fText: $"-{f}"));

            var flagsText = string.Join(' ', posFlagNames.Concat(negFlagNames).OrderBy(f => f.flag).Select(p => p.fText));
            return flagsText;
        }
    }
}
