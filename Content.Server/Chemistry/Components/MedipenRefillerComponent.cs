using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Chemistry;
using Robust.Shared.Audio;

namespace Content.Server.Chemistry.Components;

[RegisterComponent]
[Access(typeof(MedipenRefillerSystem))]
public sealed partial class MedipenRefillerComponent : Component
{
    [DataField("isEmmaged"), ViewVariables(VVAccess.ReadOnly)]
    public bool IsEmmaged;

    [DataField("clickSound"), ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");
}
