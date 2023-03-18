using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Content.Server.Speech.EntitySystems;

namespace Content.Server.Speech.Components;

[RegisterComponent]
public sealed class PirateAccentComponent : Component
{
    [DataField("yarChance")]
    public float YarChance = 1f;

    [DataField("pirateWord", customTypeSerializer: typeof(PrototypeIdSerializer<ReplacementAccentPrototype>))]
    public string PirateWord = "accent-pirate-word-1";

    [DataField("pirateResponse", customTypeSerializer: typeof(PrototypeIdSerializer<ReplacementAccentPrototype>))]
    public string PirateResponse = "accent-pirate-word-2";

    [DataField("pirateWordOne", customTypeSerializer: typeof(PrototypeIdSerializer<ReplacementAccentPrototype>))]
    public string PirateWordOne = "accent-pirate-word-3";

    [DataField("pirateResponseOne", customTypeSerializer: typeof(PrototypeIdSerializer<ReplacementAccentPrototype>))]
    public string PirateResponseOne = "accent-pirate-word-4";

    [DataField("pirateWordTwo", customTypeSerializer: typeof(PrototypeIdSerializer<ReplacementAccentPrototype>))]
    public string PirateWordTwo = "accent-pirate-word-5";

    [DataField("pirateResponseTwo", customTypeSerializer: typeof(PrototypeIdSerializer<ReplacementAccentPrototype>))]
    public string PirateResponseTwo = "accent-pirate-word-6";

    [DataField("pirateWordThree", customTypeSerializer: typeof(PrototypeIdSerializer<ReplacementAccentPrototype>))]
    public string PirateWordThree = "accent-pirate-word-7";

    [DataField("pirateResponseThree", customTypeSerializer: typeof(PrototypeIdSerializer<ReplacementAccentPrototype>))]
    public string PirateResponseThree = "accent-pirate-word-8";

    [DataField("pirateWordFour", customTypeSerializer: typeof(PrototypeIdSerializer<ReplacementAccentPrototype>))]
    public string PirateWordFour = "accent-pirate-word-9";

    [DataField("pirateWordFive", customTypeSerializer: typeof(PrototypeIdSerializer<ReplacementAccentPrototype>))]
    public string PirateWordFive = "accent-pirate-word-10";

    [DataField("piratePrefix", customTypeSerializer: typeof(PrototypeIdSerializer<ReplacementAccentPrototype>))]
    public string PiratePrefix = "accent-pirate-prefix-";
}
