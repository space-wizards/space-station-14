using Content.Server.Polymorph.Systems;
using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;

namespace Content.Server.Polymorph.Components;

[RegisterComponent]
[Access(typeof(PolymorphSystem))]
public sealed partial class PolymorphableComponent : Component
{
    /// <summary>
    /// A list of all the polymorphs that the entity has.
    /// Used to manage them and remove them if needed.
    /// </summary>
    public Dictionary<ProtoId<PolymorphPrototype>, EntityUid>? PolymorphActions = null;

    /// <summary>
    /// Timestamp for when the most recent polymorph ended.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan? LastPolymorphEnd = null;

        /// <summary>
    /// The polymorphs that the entity starts out being able to do.
    /// </summary>
    [DataField]
    public List<ProtoId<PolymorphPrototype>>? InnatePolymorphs;
}
