using Robust.Shared.Prototypes;

namespace Content.Shared.CosmicCult.Components;

/// <summary>
/// Indicates that an entity will be converted to the given prototype when corrupted by the Cosmic Cult
/// </summary>
[RegisterComponent]
public sealed partial class CosmicCorruptibleComponent : Component
{
    [DataField(required: true)]
    public EntProtoId ConvertTo;
}
