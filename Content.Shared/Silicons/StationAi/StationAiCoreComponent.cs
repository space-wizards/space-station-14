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
    /// When false, the AI is being projected into a local area, such as a holopad
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Remote = true;

    /// <summary>
    /// The invisible eye entity being used to look around.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? RemoteEntity;

    /// <summary>
    /// Prototype that represents the 'eye' of the AI
    /// </summary>
    [DataField(readOnly: true)]
    public EntProtoId? RemoteEntityProto = "StationAiHolo";

    /// <summary>
    /// Prototype that represents the physical avatar of the AI
    /// </summary>
    [DataField(readOnly: true)]
    public EntProtoId? PhysicalEntityProto = "StationAiHoloLocal";

    public const string Container = "station_ai_mind_slot";
}

/// <summary>
/// This event is raised on a station AI 'eye' that is being replaced with a new one 
/// </summary>
/// <param name="NewRemoteEntity">The entity UID of the replacement entity</param>
[ByRefEvent]
public record struct StationAiRemoteEntityReplacementEvent(EntityUid? NewRemoteEntity);
