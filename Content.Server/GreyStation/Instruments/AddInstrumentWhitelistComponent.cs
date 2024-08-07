using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server.GreyStation.Instruments;

/// <summary>
/// Adds a tag to the user's instrument whitelist then removes itself from the entity.
/// </summary>
[RegisterComponent, Access(typeof(InstrumentWhitelistSystem))]
public sealed partial class AddInstrumentWhitelistComponent : Component
{
    /// <summary>
    /// The tag to add to the whitelist.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<TagPrototype> Tag;
}
