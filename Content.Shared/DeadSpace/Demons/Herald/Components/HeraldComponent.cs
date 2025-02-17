// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;
using Robust.Shared.Audio;
using Content.Shared.FixedPoint;

namespace Content.Shared.DeadSpace.Demons.Herald.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class HeraldComponent : Component
{

    [DataField]
    public string ActionHeraldEnrage = "ActionHeraldEnrage";

    [DataField]
    public EntityUid? ActionHeraldEnrageEntity;

    [ViewVariables(VVAccess.ReadWrite)]
    public float MovementSpeedBuff = 2f;

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan EnrageDuration = TimeSpan.FromSeconds(60);

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan EnrageTime = TimeSpan.FromSeconds(0);

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan TimeUtilDead = TimeSpan.Zero;

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan DeadDuration = TimeSpan.FromSeconds(1);

    [DataField("enrage")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool isEnrage = false;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool IsDead = false;

    #region Visualizer
    [DataField("state")]
    public string State = "herald";

    [DataField("enragingState")]
    public string EnragingState = "enraging";
    #endregion

    [ViewVariables(VVAccess.ReadWrite), DataField("soundRoar")]
    public SoundSpecifier? SoundRoar =
        new SoundPathSpecifier("/Audio/Animals/space_dragon_roar.ogg")
        {
            Params = AudioParams.Default.WithVolume(3f),
        };

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public FixedPoint2 DamageModifier = FixedPoint2.New(2);
}
