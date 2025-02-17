// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Audio;

namespace Content.Shared.DeadSpace.Abilities.Invisibility.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class InvisibilityComponent : Component
{
    [DataField("actionInvisibility", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionInvisibility = "ActionInvisibility";

    [DataField("actionInvisibilityEntity")]
    public EntityUid? ActionInvisibilityEntity;

    [DataField("InvisibilitySound")]
    public SoundSpecifier? InvisibilitySound = default;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsInvisible = false;

    [DataField("visibility"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float Visibility = 0.5f;

    [DataField]
    public float MinVisibility = -1f;

    [DataField]
    public float MaxVisibility = 1.5f;
}
