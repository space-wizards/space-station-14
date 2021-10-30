using System;
using System.Collections.Generic;
using Content.Server.GameTicking.Rules;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameTicking
{
    public partial class GameTicker
    {
        [ViewVariables] private readonly List<GameRule> _gameRules = new();
        public IEnumerable<GameRule> ActiveGameRules => _gameRules;

        public T AddGameRule<T>() where T : GameRule, new()
        {
            var instance = _dynamicTypeFactory.CreateInstance<T>();

            _gameRules.Add(instance);
            instance.Added();

            RaiseLocalEvent(new GameRuleAddedEvent(instance));

            return instance;
        }

        public bool HasGameRule(string? name)
        {
            if (name == null)
                return false;

            foreach (var rule in _gameRules)
            {
                if (rule.GetType().Name == name)
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasGameRule(Type? type)
        {
            if (type == null || !typeof(GameRule).IsAssignableFrom(type))
                return false;

            foreach (var rule in _gameRules)
            {
                if (rule.GetType().IsAssignableFrom(type))
                    return true;
            }

            return false;
        }

        public void RemoveGameRule(GameRule rule)
        {
            if (_gameRules.Contains(rule)) return;

            rule.Removed();

            _gameRules.Remove(rule);
        }
    }

    public class GameRuleAddedEvent
    {
        public GameRule Rule { get; }

        public GameRuleAddedEvent(GameRule rule)
        {
            Rule = rule;
        }
    }
}
