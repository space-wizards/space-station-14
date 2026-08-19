using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;

namespace Content.Server.Objectives.Components;

/// <summary>
/// This is used to blacklist certain traitor profiles (as declared in AutoTraitorComponent) from being given certain objectives.<br/>
/// Attach this component to an objective.
/// Can declare <c>profiles: [TraitorReinforcement]</c> to stop syndie reinforcements from being given that objective.
/// </summary>
[RegisterComponent]
public sealed partial class TraitorProfileBlacklistComponent : Component
{
    [DataField(required: true)]
    public HashSet<EntProtoId> Profiles;
}