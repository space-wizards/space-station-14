// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.DeadSpace.Traps;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class BearTrapComponent : Component
{
    [DataField]
    public TimeSpan ArmingTime = TimeSpan.FromSeconds(8);

    [DataField]
    public TimeSpan StunTime = TimeSpan.FromSeconds(10);

    [DataField]
    public float MinimumOpacity = 0.35f;

    [DataField]
    public TimeSpan DisarmTime = TimeSpan.FromSeconds(15);

    [DataField]
    public DamageSpecifier Damage = new()
    {
        DamageDict = { { "Slash", 50 } },
    };

    [AutoNetworkedField, ViewVariables]
    public bool Arming;

    [AutoNetworkedField, ViewVariables]
    public bool Armed;

    [AutoNetworkedField, ViewVariables]
    public bool Used;

    [AutoNetworkedField, ViewVariables]
    public float Opacity = 1f;

    [ViewVariables]
    public TimeSpan? ArmsAt;

    [ViewVariables]
    public EntityUid? Installer;
}

[Serializable, NetSerializable]
public enum BearTrapVisuals : byte
{
    Armed,
}

[Serializable, NetSerializable]
public sealed partial class BearTrapDisarmDoAfterEvent : SimpleDoAfterEvent;
