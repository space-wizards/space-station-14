using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Inventory;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Clothing.Components;


[Access(typeof(InjectSystem))]
[RegisterComponent]
public sealed partial class InjectComponent : Component
{

    public bool Locked = true;
    [DataField("action", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Action = "ActionToggleInjection";
    [DataField("injectAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string InjectAction = "ActionInjection";

    [DataField("actionEntity")]
    public EntityUid? ActionEntity;

    [DataField("injectEntity")]
    public EntityUid? InjectActionEntity;

    [DataField("requiredSlot")]
    public SlotFlags RequiredFlags = SlotFlags.OUTERCLOTHING;

    [DataField("containerId")]
    public string ContainerId = "beakerSlot";

    [ViewVariables]
    public ContainerSlot? Container;

    [DataField("delay")]
    public TimeSpan? Delay = TimeSpan.FromSeconds(30);

    [DataField("stripDelay")]
    public TimeSpan? StripDelay = TimeSpan.FromSeconds(10);

    [DataField("verbText")]
    public string VerbText = "hardsuitinjection-toggle";
    [DataField("injectSound")]
    public SoundSpecifier InjectSound = new SoundPathSpecifier("/Audio/Items/hypospray.ogg");
}
