using Content.Shared.Body.Part;
using Content.Shared.Body.Prototypes;
using Content.Shared.DragDrop;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Body.Components;

[RegisterComponent, NetworkedComponent]
public sealed class BodyComponent : Component, IDraggable
{
    [ViewVariables]
    [DataField("prototype", customTypeSerializer: typeof(PrototypeIdSerializer<BodyPrototype>))]
    public readonly string? Prototype;

    [ViewVariables]
    [DataField("parent")]
    public BodyPartSlot? ParentSlot;

    [ViewVariables]
    [DataField("children")]
    public Dictionary<string, BodyPartSlot> Children = new();

    [ViewVariables]
    [DataField("partType")]
    public BodyPartType PartType = BodyPartType.Other;

    // TODO BODY Replace with a simulation of organs
    /// <summary>
    ///     Whether or not the owning <see cref="Body"/> will die if all
    ///     <see cref="BodyComponent"/>s of this type are removed from it.
    /// </summary>
    [ViewVariables]
    [DataField("vital")]
    public bool IsVital;

    [ViewVariables]
    [DataField("symmetry")]
    public BodyPartSymmetry Symmetry = BodyPartSymmetry.None;

    [ViewVariables]
    [DataField("attachable")]
    public bool Attachable = true;

    [ViewVariables]
    [DataField("organ")]
    public bool Organ;

    [ViewVariables]
    [DataField("gibSound")]
    public SoundSpecifier GibSound = new SoundCollectionSpecifier("gib");

    bool IDraggable.CanStartDrag(StartDragDropEvent args)
    {
        return true;
    }

    bool IDraggable.CanDrop(CanDropEvent args)
    {
        return true;
    }
}
