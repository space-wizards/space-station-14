using Content.Shared.Lathe.Prototypes;
using Content.Shared.Research.Prototypes;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Xenoborgs.Components;

/// <summary>
/// Enables an entity to convert bodies into Xenoborgs.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedXenoborgFactorySystem))]
public sealed partial class XenoborgFactoryComponent : Component
{
    /// <summary>
    /// a blacklist for what entities cannot be inserted into this reclaimer
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// Recipes that this factory can produce.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<LatheRecipePackPrototype> BorgRecipePack = "EmptyXenoborgs";

    /// <summary>
    /// whether or not we cut off the sound early when the reclaiming ends.
    /// </summary>
    [DataField]
    public bool CutOffSound = true;

    /// <summary>
    /// An "enable" toggle for things like interfacing with machine linking
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// The fixture that starts reclaiming on collision.
    /// </summary>
    [DataField]
    public string FixtureId = "XenoborgFactoryFixture";

    /// <summary>
    /// A counter of how many items have been processed
    /// </summary>
    /// <remarks>
    /// I saw this on the recycler and i'm porting it because it's cute af
    /// </remarks>
    [DataField, AutoNetworkedField]
    public int ItemsProcessed;

    /// <summary>
    /// When the next sound will be allowed to be played. Used to prevent spam.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextSound;

    /// <summary>
    /// Default chassis type to produce.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<LatheRecipePrototype> Recipe = "XenoborgEngiRecipe";

    /// <summary>
    /// The sound played when something is being processed.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound;

    /// <summary>
    /// Minimum time inbetween each <see cref="Sound"/>
    /// </summary>
    [DataField]
    public TimeSpan SoundCooldown = TimeSpan.FromSeconds(0.8f);

    public EntityUid? Stream;

    /// <summary>
    /// a whitelist for what entities can be inserted into this reclaimer
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;
}
