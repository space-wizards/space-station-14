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

    [DataField]
    public SoundSpecifier CremateStartSound = new SoundPathSpecifier("/Audio/Items/Lighters/lighter1.ogg");

    [DataField]
    public SoundSpecifier CrematingSound = new SoundPathSpecifier("/Audio/Effects/burning.ogg");

    [DataField]
    public SoundSpecifier CremateFinishSound = new SoundPathSpecifier("/Audio/Machines/ding.ogg");
}
