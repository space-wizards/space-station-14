using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Glue;

/// <summary>
/// This component gets attached to an item that has been glued.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(GlueSystem))]
public sealed partial class GluedComponent : Component
{
    /// <summary>
    /// The TimeSpan this effect expires at.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan Until;

    /// <summary>
    /// The duration this effect will last. Determined by the quantity of the reagent that is applied.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField]
    public TimeSpan Duration;
}
