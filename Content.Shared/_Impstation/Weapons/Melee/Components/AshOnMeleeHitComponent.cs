using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Weapons.Melee;

/// <summary>
/// Ashes the target on melee hits.
/// </summary>
[RegisterComponent]
public sealed partial class AshOnMeleeHitComponent : Component
{
    [DataField("spawn")]
    public EntProtoId AshPrototype = "Ash";

    /// <summary>
    /// The popup that appears upon ashing.
    /// </summary>
    [DataField]
    public string Popup = "ash-on-melee-generic";

    /// <summary>
    /// The sound played upon ashing.
    /// </summary>
    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_EE/Supermatter/supermatter.ogg");

    /// <summary>
    /// Whether the entity deletes itself after ashing something.
    /// </summary>
    [DataField]
    public bool SingleUse = true;
}
