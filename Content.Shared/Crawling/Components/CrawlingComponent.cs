using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Crawling;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(CrawlingSystem))]
public sealed partial class CrawlingComponent : Component
{
    [ViewVariables, DataField("sprintSpeedModifier"), AutoNetworkedField]
    public float SprintSpeedModifier = 0.3f;

    [ViewVariables, DataField("walkSpeedModifier"), AutoNetworkedField]
    public float WalkSpeedModifier = 0.3f;
}
