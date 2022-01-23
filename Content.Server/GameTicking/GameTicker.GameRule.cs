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

        public bool AddGameRule(GameRulePrototype rule)
        {
            if (!_gameRules.Add(rule))
                return false;

            RaiseLocalEvent(new GameRuleAddedEvent(rule));
            return true;
        }

        public bool RemoveGameRule(GameRulePrototype rule)
        {
            if (!_gameRules.Remove(rule))
                return false;

            RaiseLocalEvent(new GameRuleRemovedEvent(rule));
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
                RemoveGameRule(rule);
            }
        }
    }

    public class GameRuleAddedEvent
    {
        public GameRulePrototype Rule { get; }

        public GameRuleAddedEvent(GameRulePrototype rule)
        {
            Rule = rule;
        }
    }

    public class GameRuleRemovedEvent
    {
        public GameRulePrototype Rule { get; }

        public GameRuleRemovedEvent(GameRulePrototype rule)
        {
            Rule = rule;
        }
    }
}
