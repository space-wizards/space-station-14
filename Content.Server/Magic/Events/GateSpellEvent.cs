using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Magic.Events;

public sealed class GateSpellEvent : InstantActionEvent
{
    [ViewVariables, DataField("firstPortal")]
    public EntityUid? FirstPortal = null;

    [ViewVariables, DataField("secondPortal")]
    public EntityUid? SecondPortal = null;

    [ViewVariables(VVAccess.ReadWrite)]
    public float SpawnAmount = 1;

    [DataField("firstPortalPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string FirstPortalPrototype = "PortalGreeny";

    [DataField("secondPortalPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string SecondPortalPrototype = "PortalYellow";

    [DataField("newPortalSound")]
    public SoundSpecifier NewPortalSound = new SoundPathSpecifier("/Audio/Magic/wand_teleport.ogg")
    {
        Params = AudioParams.Default.WithVolume(-2f)
    };

    [DataField("clearPortalsSound")]
    public SoundSpecifier ClearPortalsSound = new SoundPathSpecifier("/Audio/Magic/wand_teleport.ogg");
}
