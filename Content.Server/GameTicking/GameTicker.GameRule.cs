using System.Collections.Generic;
using System.Linq;
using Content.Server.GameTicking.Rules;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameTicking
{
    public sealed partial class GameTicker
    {
        // No duplicates.
        [ViewVariables] private readonly HashSet<GameRulePrototype> _addedGameRules = new();
        public IEnumerable<GameRulePrototype> AddedGameRules => _addedGameRules;

        [ViewVariables] private readonly HashSet<GameRulePrototype> _startedGameRules = new();
        public IEnumerable<GameRulePrototype> StartedGameRules => _startedGameRules;

        /// <summary>
        ///     Game rules can be 'started' separately from being added. 'Starting' them usually
        ///     happens at round start while they can be added and removed before then.
        /// </summary>
        public void StartGameRule(GameRulePrototype rule)
        {
            if (!GameRuleAdded(rule))
                AddGameRule(rule);

            if (_startedGameRules.Add(rule))
                RaiseLocalEvent(new GameRuleStartedEvent(rule));
        }

        /// <summary>
        ///     Ends a game rule.
        ///     This always includes removing it (removing it from added game rules) so that behavior
        ///     is not separate from this.
        /// </summary>
        /// <param name="rule"></param>
        public void EndGameRule(GameRulePrototype rule)
        {
            if (!GameRuleAdded(rule))
                return;

            _addedGameRules.Remove(rule);

            if (GameRuleStarted(rule))
                _startedGameRules.Remove(rule);
            RaiseLocalEvent(new GameRuleEndedEvent(rule));
        }

        /// <summary>
        ///     Adds a game rule to the list, but does not
        ///     start it yet, instead waiting until roundstart.
        /// </summary>
        public bool AddGameRule(GameRulePrototype rule)
        {
            if (!_addedGameRules.Add(rule))
                return false;

            RaiseLocalEvent(new GameRuleAddedEvent(rule));
            return true;
        }

        public bool GameRuleAdded(GameRulePrototype rule)
        {
            return _addedGameRules.Contains(rule);
        }

        public bool GameRuleAdded(string rule)
        {
            foreach (var ruleProto in _addedGameRules)
            {
                if (ruleProto.ID.Equals(rule))
                    return true;
            }

            return false;
        }

        public bool GameRuleStarted(GameRulePrototype rule)
        {
            return _startedGameRules.Contains(rule);
        }

        public bool GameRuleStarted(string rule)
        {
            foreach (var ruleProto in _startedGameRules)
            {
                if (ruleProto.ID.Equals(rule))
                    return true;
            }

            return false;
        }

        public void ClearGameRules()
        {
            foreach (var rule in _addedGameRules.ToArray())
            {
                EndGameRule(rule);
            }
        }
    }

    /// <summary>
    ///     Raised broadcast when a game rule is selected, but not started yet.
    /// </summary>
    public sealed class GameRuleAddedEvent
    {
        public GameRulePrototype Rule { get; }

        public GameRuleAddedEvent(GameRulePrototype rule)
        {
            Rule = rule;
        }
    }

    public sealed class GameRuleStartedEvent
    {
        public GameRulePrototype Rule { get; }

        public GameRuleStartedEvent(GameRulePrototype rule)
        {
            Rule = rule;
        }
    }

    public sealed class GameRuleEndedEvent
    {
        public GameRulePrototype Rule { get; }

        public GameRuleEndedEvent(GameRulePrototype rule)
        {
            Rule = rule;
        }
    }
}
