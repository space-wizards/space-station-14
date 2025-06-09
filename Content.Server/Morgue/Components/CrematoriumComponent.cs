using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;

namespace Content.Server.Morgue.Components;

[RegisterComponent]
public sealed partial class CrematoriumComponent : Component
{
    /// <summary>
    ///     The time it takes to cook in second
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public int CookTime = 5;

    [DataField("cremateStartSound")]
    public SoundSpecifier CremateStartSound = new SoundPathSpecifier("/Audio/Items/Lighters/lighter1.ogg");

    [DataField("crematingSound")]
    public SoundSpecifier CrematingSound = new SoundPathSpecifier("/Audio/Effects/burning.ogg");

    [DataField("cremateFinishSound")]
    public SoundSpecifier CremateFinishSound = new SoundPathSpecifier("/Audio/Machines/ding.ogg");

    [ViewVariables(VVAccess.ReadWrite)]
    public bool Powered; // imp

    /// <summary>
    /// Stores entity of <see cref="CrematingSoundEntity"/> to allow ending it early when power is interrupted.
    /// </summary>
    [DataField]
    public (EntityUid, AudioComponent)? CrematingSoundEntity; // imp
}
