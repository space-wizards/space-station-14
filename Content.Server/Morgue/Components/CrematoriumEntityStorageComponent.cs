using System.Threading;
using Content.Server.Storage.Components;
using Content.Shared.Sound;

namespace Content.Server.Morgue.Components;

[RegisterComponent]
public sealed class CrematoriumEntityStorageComponent : Component
{
    [ViewVariables]
    public bool Cooking;

    [ViewVariables(VVAccess.ReadWrite)]
    public int BurnMilis = 5000;

    public CancellationTokenSource? CremateCancelToken;

    [DataField("cremateStartSound")]
    public SoundSpecifier CremateStartSound = new SoundPathSpecifier("/Audio/Items/lighter1.ogg");

    [DataField("crematingSound")]
    public SoundSpecifier CrematingSound = new SoundPathSpecifier("/Audio/Effects/burning.ogg");

    [DataField("cremateFinishSound")]
    public SoundSpecifier CremateFinishSound = new SoundPathSpecifier("/Audio/Machines/ding.ogg");
}
