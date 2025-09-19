using System.Diagnostics.CodeAnalysis;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Species;

/// <summary>
/// Abstract class for playtime and other requirements for roundstart species.
/// </summary>
[ImplicitDataDefinitionForInheritors]
[Serializable, NetSerializable]
public abstract partial class SpeciesRestriction
{
    [DataField]
    public bool Inverted;

    public abstract bool Check(
        IEntityManager entManager,
        IPrototypeManager protoManager,
        HumanoidCharacterProfile? profile,
        IReadOnlyDictionary<string, TimeSpan> playTimes,
        [NotNullWhen(false)] out FormattedMessage? reason);
}

