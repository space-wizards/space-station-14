using Content.Shared.Sound;
using System.Threading;

namespace Content.Server.Morgue.Components;

[RegisterComponent]
public sealed class CrematoriumComponent : Component
{
    /// <summary>
    ///     Whether or not the crematorium is currently cooking
    /// </summary>
    [ViewVariables]
    public bool Cooking;

    /// <summary>
    ///     The time it takes to cook
    /// </summary>
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
