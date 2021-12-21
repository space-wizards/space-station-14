using System;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.GameTicking.Rules;

[PublicAPI]
public abstract class GameRuleSystem : EntitySystem
{
    [Dependency] protected GameTicker GameTicker = default!;

    /// <summary>
    ///     Whether this GameRule is currently enabled or not.
    ///     Be sure to check this before doing anything rule-specific.
    /// </summary>
    public bool Enabled { get; protected set; } = false;

    /// <summary>
    ///     When the GameRule prototype with this ID is added, this system will be enabled.
    ///     When it gets removed, this system will be disabled.
    /// </summary>
    public abstract string Prototype { get; }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GameRuleAddedEvent>(OnGameRuleAdded);
        SubscribeLocalEvent<GameRuleRemovedEvent>(OnGameRuleRemoved);

    }

    private void OnGameRuleAdded(GameRuleAddedEvent ev)
    {
        if (ev.Rule.ID != Prototype)
            return;

        Enabled = true;
        Added();
    }

    private void OnGameRuleRemoved(GameRuleRemovedEvent ev)
    {
        if (ev.Rule.ID != Prototype)
            return;

        Enabled = false;
        Removed();
    }

    /// <summary>
    ///     Called when the game rule has been added and this system has been enabled.
    /// </summary>
    public abstract void Added();

    /// <summary>
    ///     Called when the game rule has been removed and this system has been disabled.
    /// </summary>
    public abstract void Removed();
}
