using Content.Server.GameTicking.Rules.Configurations;
using JetBrains.Annotations;

namespace Content.Server.GameTicking.Rules;

[PublicAPI]
public abstract class GameRuleSystem : EntitySystem
{
    [Dependency] protected GameTicker GameTicker = default!;

    /// <summary>
    ///     Whether this GameRule is currently added or not.
    ///     Be sure to check this before doing anything rule-specific.
    /// </summary>
    public bool RuleAdded { get; protected set; }

    /// <summary>
    ///     Whether this game rule has been started after being added.
    ///     You probably want to check this before doing any update loop stuff.
    /// </summary>
    public bool RuleStarted { get; protected set; }

    /// <summary>
    ///     When the GameRule prototype with this ID is added, this system will be enabled.
    ///     When it gets removed, this system will be disabled.
    /// </summary>
    public new abstract string Prototype { get; }

    /// <summary>
    ///     Holds the current configuration after the event has been added.
    ///     This should not be getting accessed before the event is enabled, as usual.
    /// </summary>
    public GameRuleConfiguration Configuration = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GameRuleAddedEvent>(OnGameRuleAdded);

        SubscribeLocalEvent<GameRuleStartedEvent>(OnGameRuleStarted);
        SubscribeLocalEvent<GameRuleEndedEvent>(OnGameRuleEnded);
    }

    private void OnGameRuleAdded(GameRuleAddedEvent ev)
    {
        if (ev.Rule.Configuration.Id != Prototype)
            return;

        Configuration = ev.Rule.Configuration;
        RuleAdded = true;

        Added();
    }

    private void OnGameRuleStarted(GameRuleStartedEvent ev)
    {
        if (ev.Rule.Configuration.Id != Prototype)
            return;

        RuleStarted = true;

        Started();
    }

    private void OnGameRuleEnded(GameRuleEndedEvent ev)
    {
        if (ev.Rule.Configuration.Id != Prototype)
            return;

        RuleAdded = false;
        RuleStarted = false;
        Ended();
    }

    /// <summary>
    ///     Called when the game rule has been added.
    ///     You should avoid using this in favor of started--they are not the same thing.
    /// </summary>
    /// <remarks>
    ///     This is virtual because it doesn't actually have to be used, and most of the time shouldn't be.
    /// </remarks>
    public virtual void Added() { }

    /// <summary>
    ///     Called when the game rule has been started.
    /// </summary>
    public abstract void Started();

    /// <summary>
    ///     Called when the game rule has ended.
    /// </summary>
    public abstract void Ended();
}
