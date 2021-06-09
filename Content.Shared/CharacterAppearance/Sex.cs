#nullable enable
using System;
using Content.Shared.Dataset;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Shared.CharacterAppearance
{
    public enum Sex
    {
        Male,
        Female
    }

    public static class SexExtensions
    {
        public static DatasetPrototype FirstNames(this Sex sex, IPrototypeManager? prototypeManager = null)
        {
            prototypeManager ??= IoCManager.Resolve<IPrototypeManager>();

            switch (sex)
            {
                case Sex.Male:
                    return prototypeManager.Index<DatasetPrototype>("names_first_male");
                case Sex.Female:
                    return prototypeManager.Index<DatasetPrototype>("names_first_female");
                default:
                    throw new ArgumentOutOfRangeException(nameof(sex), sex, null);
            }
        }
    }
}
