using System;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.GameTicking.Rules;

[PublicAPI]
public abstract class GameRuleSystem : EntitySystem
{
    [Dependency] protected GameTicker GameTicker = default!;

    public bool Enabled { get; protected set; } = false;

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

    public abstract void Added();
    public abstract void Removed();
}
