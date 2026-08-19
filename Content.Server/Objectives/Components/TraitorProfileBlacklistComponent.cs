using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;

namespace Content.Server.Objectives.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class TraitorProfileBlacklistComponent : Component
{
    [DataField(required: true)]
    public HashSet<EntProtoId> Profiles;
}