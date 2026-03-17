using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Stacks;

/// <summary>
/// Component on an entity that represents a stack of identical things, usually materials.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedStackSystem))]
public sealed partial class StackComponent : Component
{
    /// <summary>
    /// What stack type we are.
    /// </summary>
    [DataField("stackType", required: true)]
    public ProtoId<StackPrototype> StackTypeId = default!;

    /// <summary>
    /// Current stack count.
    /// Do NOT set this directly, use the <see cref="SharedStackSystem.SetCount"/> method instead.
    /// </summary>
    [DataField]
    public int Count = 30;

    /// <summary>
    /// Max amount of things that can be in the stack.
    /// Overrides the max defined on the stack prototype.
    /// </summary>
    [DataField]
    public int? MaxCountOverride;

    /// <summary>
    /// Set to true to not reduce the count when used.
    /// </summary>
    [DataField]
    public bool Unlimited;

    /// <summary>
    /// When throwing this item, do we want to only throw one part of the stack or the whole stack at once?
    /// </summary>
    [DataField]
    public bool ThrowIndividually;

    /// <summary>
    /// Used by StackStatusControl in client to update UI.
    /// </summary>
    [ViewVariables]
    [Access(typeof(SharedStackSystem), Other = AccessPermissions.ReadWrite)] // Set by StackStatusControl
    public bool UiUpdateNeeded { get; set; }

    /// <summary>
    ///     Default IconLayer stack.
    /// </summary>
    [DataField]
    public string BaseLayer = "";

    /// <summary>
    /// Determines if the visualizer uses composite or non-composite layers for icons. Defaults to false.
    ///
    /// <list type="bullet">
    /// <item>
    /// <description>false: they are opaque and mutually exclusive (e.g. sprites in a cable coil). <b>Default value</b></description>
    /// </item>
    /// <item>
    /// <description>true: they are transparent and thus layered one over another in ascending order first</description>
    /// </item>
    /// </list>
    ///
    /// </summary>
    [DataField("composite")]
    public bool IsComposite;

    /// <summary>
    /// Sprite layers used in stack visualizer. Sprites first in layer correspond to lower stack states
    /// e.g. <code>_spriteLayers[0]</code> is lower stack level than <code>_spriteLayers[1]</code>.
    /// </summary>
    [DataField]
    public List<string> LayerStates = new();

    /// <summary>
    /// An optional function to convert the amounts used to adjust a stack's appearance.
    /// Useful for different denominations of cash, for example.
    /// </summary>
    [DataField]
    public StackLayerFunction LayerFunction = StackLayerFunction.None;
}

[Serializable, NetSerializable]
public sealed class StackComponentState : ComponentState
{
    public int Count { get; }
    public int? MaxCountOverride { get; }
    public bool Unlimited { get; }

    public StackComponentState(int count, int? maxCountOverride, bool unlimited)
    {
        Count = count;
        MaxCountOverride = maxCountOverride;
        Unlimited = unlimited;
    }
}

[Serializable, NetSerializable]
public enum StackLayerFunction : byte
{
    // <summary>
    // No operation performed.
    // </summary>
    None,

    // <summary>
    // Arbitrarily thresholds the stack amount for each layer.
    // Expects entity to have StackLayerThresholdComponent.
    // </summary>
    Threshold
}
