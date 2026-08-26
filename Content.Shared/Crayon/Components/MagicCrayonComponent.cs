using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Crayon;

/// <summary>
/// A component that describes a magic crayon, usually held by a mime.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MagicCrayonComponent : Component
{
    /// <summary>
    /// The fake food prototype that will be spawned by this magic crayon.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId FakeFood = "FakeBurgerBacon";

    /// <summary>
    /// What to replace the magic crayon with when it's been used up.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId NormalCrayon = "CrayonMime";

    /// <summary>
    /// How long it takes to spawn a fake consumable with this magic crayon.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan SpawnDelay = TimeSpan.FromSeconds(7f);

    /// <summary>
    /// If not null, the sound to play when the fake food is spawned.
    /// </summary>
    [DataField("spawnSound"), AutoNetworkedField]
    public SoundSpecifier? OnSpawnSound = new SoundPathSpecifier("/Audio/Effects/pop.ogg");
}
