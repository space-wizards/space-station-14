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

    [DataField("pirateAccent", customTypeSerializer: typeof(PrototypeIdSerializer<ReplacementAccentPrototype>))]
    public readonly string PirateAccent = "pirate";
}
