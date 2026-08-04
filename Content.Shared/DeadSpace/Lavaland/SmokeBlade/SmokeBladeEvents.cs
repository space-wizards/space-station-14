// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT
using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeadSpace.Lavaland.SmokeBlade;

public sealed partial class SmokeBladeActionEvent : InstantActionEvent
{
    [DataField]
    public int Radius = 2;

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan TickInterval = TimeSpan.FromSeconds(0.1);

    [DataField]
    public SoundSpecifier AmbientSound = new SoundPathSpecifier("/Audio/Effects/burning.ogg");

    [DataField]
    public EntProtoId VisualPrototype = "LavalandBloodBladeSmokeVisual";

    [DataField]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new()
        {
            { "Slash", FixedPoint2.New(5) },
        },
    };
}