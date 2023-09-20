using Content.Shared.Body.Prototypes;
using Content.Shared.Body.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Body.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedBodySystem))]
public sealed partial class BodyComponent : Component
{
    /// <summary>
    /// Relevant template to spawn for this body.
    /// </summary>
    [DataField]
    public ProtoId<BodyPrototype>? Prototype;

    /// <summary>
    /// Container that holds the root body part.
    /// </summary>
    /// <remarks>
    /// Typically is the torso.
    /// </remarks>
    [ViewVariables] public ContainerSlot RootContainer = default!;

    [ViewVariables]
    public string RootPartSlot => RootContainer.ID;

    [DataField] public SoundSpecifier GibSound = new SoundCollectionSpecifier("gib");

    /// <summary>
    /// The amount of legs required to move at full speed.
    /// If 0, then legs do not impact speed.
    /// </summary>
    [DataField] public int RequiredLegs;

    [ViewVariables]
    [DataField]
    public HashSet<EntityUid> LegEntities = new();
}

[Serializable, NetSerializable]
public sealed class BodyComponentState : ComponentState
{
    public string? Prototype;
    public string? RootPartSlot;
    public SoundSpecifier GibSound;
    public int RequiredLegs;
    public HashSet<NetEntity> LegNetEntities;

    public BodyComponentState(string? prototype, string? rootPartSlot, SoundSpecifier gibSound,
        int requiredLegs, HashSet<NetEntity> legNetEntities)
    {
        Prototype = prototype;
        RootPartSlot = rootPartSlot;
        GibSound = gibSound;
        RequiredLegs = requiredLegs;
        LegNetEntities = legNetEntities;
    }
}
