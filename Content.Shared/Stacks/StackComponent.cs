using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Stacks;

[RegisterComponent, NetworkedComponent]
public sealed partial class StackComponent : Component
{
    [DataField("stackType", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<StackPrototype>))]
    public string StackTypeId { get; private set; } = default!;

    /// <summary>
    ///     Current stack count.
    ///     Do NOT set this directly, use the <see cref="SharedStackSystem.SetCount"/> method instead.
    /// </summary>
    [DataField]
    public int Count = 30;

    /// <summary>
    ///     Max amount of things that can be in the stack.
    ///     Overrides the max defined on the stack prototype.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField]
    public int? MaxCountOverride;

    /// <summary>
    ///     Set to true to not reduce the count when used.
    ///     Note that <see cref="Count"/> still limits the amount that can be used at any one time.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public bool Unlimited;

    /// <summary>
    /// Lingering stacks will remain present even when there are no items.
    /// Instead, they will become transparent.
    /// </summary>
    [DataField]
    public bool Lingering;

    /// <summary>
    /// Setting this to true will cause you to only throw
    /// one item of the stack at a time
    /// </summary>
    [DataField]
    public bool ThrowIndividually;

    [ViewVariables]
    public bool UiUpdateNeeded;

    /// <summary>
    /// Default IconLayer stack.
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
    public List<string> LayerStates = [];

    /// <summary>
    /// How wide of an area to search to gather items of the same stack when clicking the floor
    /// </summary>
    [DataField]
    public int AreaInsertRadius = 1;
}

[Serializable, NetSerializable]
public sealed class StackComponentState : ComponentState
{
    public int Count;
    public int? MaxCount;

    public bool Lingering;

    public StackComponentState(int count, int? maxCount, bool lingering)
    {
        Count = count;
        MaxCount = maxCount;
        Lingering = lingering;
    }
}
