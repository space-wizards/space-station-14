using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared.Chemistry.Prototypes;

/// <summary>
/// This defines the behavior of an injector.
/// Every injector requires this and it defines how much an injector injects, what transferamounts they can switch between, etc.
/// </summary>
[Prototype]
public sealed partial class InjectorModePrototype : IPrototype, IInheritingPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <inheritdoc/>
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<InjectorModePrototype>))]
    public string[]? Parents { get; private set; }

    /// <inheritdoc/>
    [AbstractDataField, NeverPushInheritance]
    public bool Abstract { get; private set; }

    /// <summary>
    /// The name of the mode that will be shown on the label UI.
    /// </summary>
    [DataField(required: true)]
    public LocId Name;

    /// <summary>
    /// If true, it'll inject the user when used in hand (Default Key: Y/Z)
    /// </summary>
    [DataField]
    public bool InjectOnUse;

    /// <summary>
    /// The transfer amounts for the set-transfer verb.
    /// </summary>
    [DataField]
    public List<FixedPoint2> TransferAmounts = new() { 1, 5, 10, 15 };

    /// <summary>
    /// Injection/Drawing delay (seconds) when the target is a mob.
    /// </summary>
    [DataField]
    public TimeSpan MobTime = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The delay to draw Reagents from Containers.
    /// If set, <see cref="RefillableSolutionComponent"/> RefillTime should probably have the same value.
    /// </summary>
    [DataField]
    public TimeSpan ContainerDrawTime = TimeSpan.Zero;


    /// <summary>
    /// The number to multiply <see cref="MobTime"/> and <see cref="DelayPerVolume"/> if the target is the downed.
    /// Downed counts as crouching, buckled on a bed or critical.
    /// </summary>
    [DataField]
    public float DownedModifier = 0.5f;

    /// <summary>
    /// The number to multiply <see cref="MobTime"/> and <see cref="DelayPerVolume"/> if the target is the user.
    /// </summary>
    [DataField]
    public float SelfModifier = 0.5f;

    /// <summary>
    /// This delay will increase the DoAfter time for each Xu above <see cref="IgnoreDelayForVolume"/>.
    /// </summary>
    [DataField]
    public TimeSpan DelayPerVolume = TimeSpan.FromSeconds(0.1);

    /// <summary>
    /// This works in tandem with <see cref="DelayPerVolume"/>.
    /// </summary>
    [DataField]
    public FixedPoint2 IgnoreDelayForVolume = FixedPoint2.New(5);

    /// <summary>
    /// What message will be displayed to the user when attempting to inject someone.
    /// </summary>
    /// <remarks>
    /// This is used for when you aren't injecting with a needle or an instant hypospray.
    /// It would be weird if someone injects with a spray, but the popup says "needle".
    /// </remarks>
    [DataField]
    public LocId PopupUserAttempt = "injector-component-needle-injecting-user";

    /// <summary>
    /// What message will be displayed to the target when someone attempts to inject into them.
    /// </summary>
    [DataField]
    public LocId PopupTargetAttempt = "injector-component-needle-injecting-target";

    /// <summary>
    /// The state of the injector. Determines its attack behavior. Containers must have the
    /// right SolutionCaps to support injection/drawing. For InjectOnly injectors this should
    /// only ever be set to Inject
    /// </summary>
    [DataField]
    public InjectorBehavior Behavior = InjectorBehavior.Inject;

    /// <summary>
    ///     Sound that will be played when injecting.
    /// </summary>
    [DataField]
    public SoundSpecifier? InjectSound;

    /// <summary>
    /// A popup for the target upon a successful injection.
    /// It's imperative that this is not null when <see cref="MobTime"/> is instant.
    /// </summary>
    [DataField]
    public LocId? InjectPopupTarget;

}

/// <summary>
/// Possible modes for an <see cref="InjectorModePrototype"/>.
/// </summary>
[Serializable, NetSerializable, Flags]
public enum InjectorBehavior
{
    /// <summary>
    /// The injector will try to inject reagent into things.
    /// </summary>
    Inject = 1 << 0,

    /// <summary>
    /// The injector will try to draw reagent from things.
    /// </summary>
    Draw = 1 << 1,

    /// <summary>
    /// The injector will draw from containers and inject into mobs.
    /// </summary>
    Dynamic = 1 << 2,
}
