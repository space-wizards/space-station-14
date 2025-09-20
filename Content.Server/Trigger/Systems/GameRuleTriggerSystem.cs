using Content.Server.Administration.Logs;
using Content.Server.GameTicking;
using Content.Shared.Database;
using Content.Shared.Trigger;
using Content.Shared.Trigger.Components.Effects;

namespace Content.Server.Trigger.Systems;

/// <summary>
/// Trigger system for game rules.
/// </summary>
public sealed class GameRuleTriggerSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AddGameRuleOnTriggerComponent, TriggerEvent>(AddRuleOnTrigger);
    }

    private void AddRuleOnTrigger(Entity<AddGameRuleOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var rule = _ticker.AddGameRule(ent.Comp.GameRule);

        _adminLogger.Add(LogType.EventStarted,
            $"{ToPrettyString(args.User):entity} added a game rule [{ent.Comp.GameRule}]" +
            $" via a trigger on {ToPrettyString(ent.Owner):entity}.");

        if (ent.Comp.StartRule && _ticker.RunLevel == GameRunLevel.InRound)
        {
            _ticker.StartGameRule(rule);
            _adminLogger.Add(LogType.EventStarted, $"{ToPrettyString(args.User):entity} started game rule [{ent.Comp.GameRule}].");
        }

        args.Handled = true;
    }
}
