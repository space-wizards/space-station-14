using Content.Shared.Body.Part;
using Content.Shared.Body.Prototypes;
using Content.Shared.Body.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using System;
using Robust.Shared.Serialization;

namespace Content.Shared.Body.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedBodySystem))]
public sealed partial class BodyComponent : Component
{
    [DataField("prototype", customTypeSerializer: typeof(PrototypeIdSerializer<BodyPrototype>)), AutoNetworkedField]
    public string? Prototype;

    [ViewVariables] public ContainerSlot RootContainer;

    [ViewVariables] public string? RootPartSlot;

    [DataField("gibSound")] public SoundSpecifier GibSound = new SoundCollectionSpecifier("gib");

    /// <summary>
    /// The amount of legs required to move at full speed.
    /// If 0, then legs do not impact speed.
    /// </summary>
    [DataField("requiredLegs")] public int RequiredLegs;

    [ViewVariables] public HashSet<EntityUid> LegEntities = new();
}

[Serializable, NetSerializable]
public sealed class BodyComponentState : ComponentState
{
    public string? Prototype;
    public string ContainerId;
    public string? RootPartSlot;
    public SoundSpecifier GibSound;
    public int RequiredLegs;
    public HashSet<NetEntity> LegNetEntities;
    public BodyComponentState(string? prototype, string containerId, string? rootPartSlot, SoundSpecifier gibSound,
        int requiredLegs, HashSet<NetEntity> legNetEntities)
    {
        Prototype = prototype;
        ContainerId = containerId;
        RootPartSlot = rootPartSlot;
        GibSound = gibSound;
        RequiredLegs = requiredLegs;
        LegNetEntities = legNetEntities;
    }
}
