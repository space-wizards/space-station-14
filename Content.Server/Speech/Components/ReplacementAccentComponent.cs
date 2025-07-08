using Content.Server.Speech.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Speech.Components;

/// <summary>
/// Replaces full sentences or words within sentences with new strings.
/// </summary>
[RegisterComponent]
public sealed partial class ReplacementAccentComponent : Component
{
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<ReplacementAccentPrototype>), required: true)]
    public string Accent = default!;

}
