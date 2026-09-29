using System.Diagnostics.CodeAnalysis;
using Content.Shared.Xenoarchaeology.Artifact.Modifiers;
using Robust.Shared.Collections;
using Robust.Shared.Serialization;

namespace Content.Shared.Xenoarchaeology.Artifact.Components;

[DataDefinition, Serializable, NetSerializable]
public sealed partial class XenoArtifactEffectsModifications
{
    [DataField]
    public Dictionary<Enum, ModifierProviderBase> Dictionary = new();

    public bool IsEmpty => Dictionary.Count <= 0;

    /// <inheritdoc cref="Dictionary{TKey,TValue}.TryGetValue"/>>
    public bool TryGetValue(Enum key, [NotNullWhen(true)] out ModifierProviderBase? value)
    {
        return Dictionary.TryGetValue(key, out value);
    }

    /// <summary>
    /// Produces new dictionary with values from both original and other, using sum of both
    /// when keys exists in both dictionaries and values are compatible.
    /// </summary>
    public static XenoArtifactEffectsModifications operator +(
        XenoArtifactEffectsModifications original,
        XenoArtifactEffectsModifications other
    )
    {
        var result = new XenoArtifactEffectsModifications();
        ValueList<Enum> alreadyMatched = new();
        foreach (var (modKey, modValue) in original.Dictionary)
        {
            if (other.Dictionary.TryGetValue(modKey, out var otherValue))
            {
                result.Dictionary[modKey] = new WrapperModifierProvider([modValue, otherValue]);
                alreadyMatched.Add(modKey);
            }
            else
            {
                result.Dictionary[modKey] = modValue;
            }
        }

        foreach (var (modKey, modValue) in other.Dictionary)
        {
            if (alreadyMatched.Contains(modKey))
                continue;

            result.Dictionary[modKey] = modValue;
        }

        return result;
    }
}
