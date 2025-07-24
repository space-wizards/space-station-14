using Robust.Shared.GameStates;

namespace Content.Shared.Stunnable;

/// <summary>
/// Knockdown as a status effect.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedStunSystem))]
public sealed partial class KnockdownStatusEffectComponent : Component
{
    /// <summary>
    /// Should this knockdown only affect crawlers?
    /// </summary>
    /// <remarks>
    /// If your status effect doesn't come paired with <see cref="StunnedStatusEffectComponent"/>
    /// Or if your status effect doesn't whitelist itself to only those with <see cref="CrawlerComponent"/>
    /// Then you need to set this to true.
    /// </remarks>
    [DataField]
    public bool Crawl;

    /// <summary>
    /// Should we drop items when we fall?
    /// </summary>
    [DataField]
    public bool Drop = true;
}
