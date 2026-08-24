using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Crayon;

[RegisterComponent]
public sealed partial class MagicCrayonComponent : Component
{
    /// <summary>
    /// The fake food prototype that will be spawned by this magic crayon.
    /// </summary>
    [DataField]
    public EntProtoId FakeFood;

    /// <summary>
    /// What to replace the magic crayon with when it's been used up.
    /// </summary>
    [DataField]
    public EntProtoId NormalCrayon;

    /// <summary>
    /// The sound to play when the fake food is spawned.
    /// </summary>
    [DataField("spawnSound")]
    public SoundSpecifier? OnSpawnSound;
}
