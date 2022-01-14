using System.Collections.Generic;
using System.Linq;
using Content.Server.GameTicking.Rules;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameTicking
{
    public partial class GameTicker
    {
        // No duplicates.
        [ViewVariables] private readonly HashSet<GameRulePrototype> _gameRules = new();
        public IEnumerable<GameRulePrototype> ActiveGameRules => _gameRules;

        public void StartGameRule(GameRulePrototype rule)
        {
            if (!HasGameRule(rule))
                EnableGameRule(rule);

            RaiseLocalEvent(new GameRuleStartedEvent(rule));
        }

        public void EndGameRule(GameRulePrototype rule)
        {
            if (!HasGameRule(rule))
                return;

            DisableGameRule(rule);
            RaiseLocalEvent(new GameRuleEndedEvent(rule));
        }

        public bool EnableGameRule(GameRulePrototype rule)
        {
            if (!_gameRules.Add(rule))
                return false;

            RaiseLocalEvent(new GameRuleEnabledEvent(rule));
            return true;
        }

        public bool DisableGameRule(GameRulePrototype rule)
        {
            if (!_gameRules.Remove(rule))
                return false;

            RaiseLocalEvent(new GameRuleDisabledEvent(rule));
            return true;
        }

        public bool HasGameRule(GameRulePrototype rule)
        {
            return _gameRules.Contains(rule);
        }

        public bool HasGameRule(string rule)
        {
            foreach (var ruleProto in _gameRules)
            {
                if (ruleProto.ID.Equals(rule))
                    return true;
            }

            return false;
        }

        public void ClearGameRules()
        {
            foreach (var rule in _gameRules.ToArray())
            {
                EndGameRule(rule);
            }
        }
    }

    public class GameRuleEnabledEvent
    {
        public GameRulePrototype Rule { get; }

        public GameRuleEnabledEvent(GameRulePrototype rule)
        {
            Rule = rule;
        }
    }

    public class GameRuleDisabledEvent
    {
        public GameRulePrototype Rule { get; }

        public GameRuleDisabledEvent(GameRulePrototype rule)
        {
            Rule = rule;
        }
    }

    public class GameRuleStartedEvent
    {
        public GameRulePrototype Rule { get; }

        public GameRuleStartedEvent(GameRulePrototype rule)
        {
            Rule = rule;
        }
    }

    public class GameRuleEndedEvent
    {
        public GameRulePrototype Rule { get; }

        public GameRuleEndedEvent(GameRulePrototype rule)
        {
            Rule = rule;
        }
    }
}
