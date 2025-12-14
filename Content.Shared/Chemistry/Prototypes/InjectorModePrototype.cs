using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Prototypes;

[Prototype("InjectorMode"), Serializable]
public sealed partial class InjectorModePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The transfer amounts for the set-transfer verb.
    /// </summary>
    [DataField]
    public List<FixedPoint2> TransferAmounts = new() { 1, 5, 10, 15 };

    /// <summary>
    /// What message will be displayed to the user when attempting to inject someone.
    /// </summary>
    /// <remarks>
    /// This is used for when you aren't injecting with a needle or an instant hypospray.
    /// It would be weird if someone injects with a spray, but the popup says "needle".
    /// </remarks>
    [DataField]
    public string PreparingInjectorUser = "injector-component-injecting-user";

    /// <summary>
    /// What message will be displayed to the target when someone attempts to inject into them.
    /// </summary>
    [DataField]
    public string PreparingInjectorTarget = "injector-component-injecting-target";

    /// <summary>
    /// The state of the injector. Determines it's attack behavior. Containers must have the
    /// right SolutionCaps to support injection/drawing. For InjectOnly injectors this should
    /// only ever be set to Inject
    /// </summary>
    [DataField]
    public InjectorToggleMode ToggleState = InjectorToggleMode.Draw;

    /// <summary>
    ///     Sound that will be played when injecting.
    /// </summary>
    [DataField]
    public SoundSpecifier? InjectSound;

    /// <summary>
    /// A popup for the target upon a successful injection.
    /// It's imperative that this is not null when <see cref="InjectTime"/> is instant.
    /// </summary>
    [DataField]
    public string? InjectPopupTarget;
}

/// <summary>
/// Possible modes for an <see cref="InjectorComponent"/>.
/// </summary>
[Serializable, NetSerializable, Flags]
public enum InjectorToggleMode
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
