using Content.Shared.Whitelist;

namespace Content.Server.Gatherable.Components;

[RegisterComponent]
[Access(typeof(GatherableSystem))]
public sealed class GatherableComponent : Component
{
    /// <summary>
    ///     Whitelist for specifying the kind of tools can be used on a resource
    ///     Supports multiple tags.
    /// </summary>
    [DataField("whitelist", required: true)]
    public EntityWhitelist? ToolWhitelist;

    /// <summary>
    ///     The amount of time in seconds it takes to complete the gathering action by hand.
    /// </summary>
    [DataField("harvestTime")]
    public float HarvestTime = 1.0f;
}
