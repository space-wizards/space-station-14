using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Silicons.StationAi;

/// <summary>
/// Indicates this entity can interact with station equipment and is a "Station AI".
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StationAiCoreComponent : Component
{
    /*
     * I couldn't think of any other reason you'd want to split these out.
     */

    /// <summary>
    /// Can it move its camera around and interact remotely with things.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Remote = true;

    /// <summary>
    /// The invisible eye entity being used to look around.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? RemoteEntity;

    [DataField(readOnly: true)]
    public EntProtoId? RemoteEntityProto = "StationAiHolo";

    public const string Container = "station_ai_mind_slot";
}
