// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Alert;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.DeadSpace.Blink;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BlinkItemComponent : Component
{
    [DataField]
    public float Range = 7f;

    [DataField]
    public float DashSpeed = 28f;

    [DataField]
    public TimeSpan DashTimeout = TimeSpan.FromSeconds(1.5);

    [DataField]
    public TimeSpan DashStallTimeout = TimeSpan.FromSeconds(0.2);

    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(15);

    [DataField]
    public TimeSpan DamageLockout = TimeSpan.FromSeconds(3);

    [DataField]
    public bool NeedHand;

    [DataField, AutoNetworkedField]
    public bool Targeting;

    [DataField]
    public ProtoId<AlertPrototype> CooldownAlert = "BlinkCooldown";

    [DataField]
    public SoundSpecifier DashSound = new SoundPathSpecifier("/Audio/_DeadSpace/Necromorfs/TheCircle/scout_blink.ogg")
    {
        Params = AudioParams.Default.WithVolume(5f),
    };

    [DataField, AutoNetworkedField]
    public TimeSpan NextUse;
}

public sealed partial class ToggleBlinkViewAlertEvent : BaseAlertEvent;

[Serializable, NetSerializable]
public sealed class BlinkRequestEvent(NetEntity item, NetCoordinates target) : EntityEventArgs
{
    public NetEntity Item = item;
    public NetCoordinates Target = target;
}

[Serializable, NetSerializable]
public sealed class BlinkDashVisualEvent(NetEntity user, TimeSpan duration) : EntityEventArgs
{
    public NetEntity User = user;
    public TimeSpan Duration = duration;
}
