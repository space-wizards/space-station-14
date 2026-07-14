using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Blocking.Components;

/// <summary>
/// This component goes on an item that you want to use to block
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BlockingComponent : Component
{
    /// <summary>
    /// The ID for the fixture that's dynamically created when blocking
    /// </summary>
    public const string BlockFixtureId = "blocking-active";

    /// <summary>
    /// The entity that's blocking
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? User;

    /// <summary>
    /// Is it currently blocking?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsBlocking;

    /// <summary>
    /// The shape of the blocking fixture that will be dynamically spawned
    /// </summary>
    [DataField]
    public IPhysShape Shape = new PhysShapeCircle(0.5f);

    /// <summary>
    /// The damage modifer to use while passively blocking
    /// </summary>
    [DataField(required: true)]
    public DamageModifierSet PassiveBlockModifier = default!;

    /// <summary>
    /// Optional damage modifier to use while actively blocking.
    /// If this is null, shield will use the PassiveBlockModifier instead.
    /// </summary>
    [DataField]
    public DamageModifierSet? ActiveBlockModifier;

    [DataField]
    public EntProtoId BlockingToggleAction = "ActionToggleBlock";

    [DataField, AutoNetworkedField]
    public EntityUid? BlockingToggleActionEntity;

    /// <summary>
    /// The sound to be played when you get hit while actively blocking
    /// </summary>
    [DataField]
    public SoundSpecifier BlockSound =
        new SoundPathSpecifier("/Audio/Weapons/block_metal1.ogg")
        {
            Params = AudioParams.Default.WithVariation(0.25f)
        };

    /// <summary>
    /// Fraction of original damage shield will take instead of user
    /// when not blocking
    /// </summary>
    [DataField]
    public float PassiveBlockFraction = 0.5f;

    /// <summary>
    /// Fraction of original damage shield will take instead of user
    /// when blocking
    /// </summary>
    [DataField]
    public float ActiveBlockFraction = 1.0f;
}
