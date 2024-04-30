using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Nutrition.Components;

[Access(typeof(SatiationSystem))]
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class SatiationComponent : Component
{
    [DataField("hunger"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public Satiation Hunger = new();

    [DataField("thirst"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public Satiation Thirst = new();

    /// <summary>
    /// The time when the thirst will update next.
    /// </summary>
    [DataField("nextUpdateTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan NextUpdateTime;

    /// <summary>
    /// The time between each update.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public TimeSpan UpdateRate = TimeSpan.FromSeconds(1);
}
