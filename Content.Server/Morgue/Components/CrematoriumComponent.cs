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
    public SoundSpecifier CremateStartSound = new SoundCollectionSpecifier("CremationStartSound");

    [DataField("crematingSound")]
    public SoundSpecifier CrematingSound = new SoundCollectionSpecifier("CremationWorkingSound");

    [DataField("cremateFinishSound")]
    public SoundSpecifier CremateFinishSound = new SoundCollectionSpecifier("CremationDoneSound");
}
