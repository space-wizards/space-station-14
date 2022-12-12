using Content.Shared.Body.Part;
using Content.Shared.Body.Prototypes;
using Content.Shared.Body.Systems;
using Content.Shared.DragDrop;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Body.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedBodySystem))]
public sealed class BodyComponent : Component, IDraggable
{
    [DataField("prototype", customTypeSerializer: typeof(PrototypeIdSerializer<BodyPrototype>))]
    public readonly string? Prototype;

    [DataField("root")]
    public BodyPartSlot? Root;

    [DataField("gibSound")]
    public SoundSpecifier GibSound = new SoundCollectionSpecifier("gib");

    /// <summary>
    /// The amount of legs required to move at full speed.
    /// If 0, then legs do not impact speed.
    /// </summary>
    [DataField("requiredLegs")]
    public int RequiredLegs;

    bool IDraggable.CanStartDrag(StartDragDropEvent args)
    {
        return true;
    }

    bool IDraggable.CanDrop(CanDropEvent args)
    {
        return true;
    }
}
