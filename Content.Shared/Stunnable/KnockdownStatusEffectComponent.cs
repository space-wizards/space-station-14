using Robust.Shared.GameStates;

namespace Content.Shared.Stunnable;

/// <summary>
/// Crawling as a status effect
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedStunSystem))]
public sealed partial class KnockdownStatusEffectComponent : Component
{
    /// <summary>
    /// Should this knockdown only affect crawlers?
    /// </summary>
    /// <remarks>
    /// This is here in case you make a status effect that affects non-crawlers, doesn't stun, and that
    /// you want to knockdown crawlers.
    /// If you set this to false, make sure you're also applying stun or whitelisting to CrawlingComponent only.
    /// If you get non-crawlers running around that's your own fault!
    /// </remarks>
    [ViewVariables]
    public bool Crawl = true;

    /// <summary>
    /// Should we drop items when we fall?
    /// </summary>
    [ViewVariables]
    public bool Drop = true;
}
