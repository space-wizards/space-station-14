using Robust.Shared.Audio;

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
    public SoundSpecifier CremateStartSound = new SoundPathSpecifier("/Audio/Items/lighter1.ogg");

    [DataField("crematingSound")]
    public SoundSpecifier CrematingSound = new SoundPathSpecifier("/Audio/Effects/burning.ogg");

    [DataField("cremateFinishSound")]
    public SoundSpecifier CremateFinishSound = new SoundPathSpecifier("/Audio/Machines/ding.ogg");
}
