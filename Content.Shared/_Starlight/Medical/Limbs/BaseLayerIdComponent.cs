using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Starlight;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BaseLayerIdComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<HumanoidSpeciesSpriteLayer>? Layer;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BaseLayerIdToggledComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<HumanoidSpeciesSpriteLayer>? Layer;
}