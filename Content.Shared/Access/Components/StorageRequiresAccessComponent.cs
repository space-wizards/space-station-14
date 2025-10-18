using Content.Shared.Access.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Access.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(StorageRequiresAccessSystem))]
public sealed partial class StorageRequiresAccessComponent : Component
{
    [DataField]
    public LocId PopupMessage = "lock-comp-has-user-access-fail";

    [DataField]
    // Yoinked from: https://github.com/discordia-space/CEV-Eris/blob/35bbad6764b14e15c03a816e3e89aa1751660ba9/sound/machines/Custom_deny.ogg
    public SoundSpecifier SoundDeny = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");
}
