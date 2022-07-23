using System.Linq;
using Content.Server.Administration;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Configurations;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.GameTicking
{
    public sealed partial class GameTicker
    {
        // No duplicates.
        [ViewVariables] private readonly HashSet<GameRulePrototype> _addedGameRules = new();

        /// <summary>
        ///     Holds all currently added game rules.
        /// </summary>
        public IReadOnlySet<GameRulePrototype> AddedGameRules => _addedGameRules;

        [ViewVariables] private readonly HashSet<GameRulePrototype> _startedGameRules = new();

        /// <summary>
        ///     Holds all currently started game rules.
        /// </summary>
        public IReadOnlySet<GameRulePrototype> StartedGameRules => _startedGameRules;

        [ViewVariables] private readonly List<(TimeSpan, GameRulePrototype)> _allPreviousGameRules = new();

        /// <summary>
        ///     A list storing the start times of all game rules that have been started this round.
        ///     Game rules can be started and stopped at any time, including midround.
        /// </summary>
        public IReadOnlyList<(TimeSpan, GameRulePrototype)> AllPreviousGameRules => _allPreviousGameRules;

        private void InitializeGameRules()
        {
            // Add game rule command.
            _consoleHost.RegisterCommand("addgamerule",
                string.Empty,
                "addgamerule <rules>",
                AddGameRuleCommand,
                AddGameRuleCompletions);

            // End game rule command.
            _consoleHost.RegisterCommand("endgamerule",
                string.Empty,
                "endgamerule <rules>",
                EndGameRuleCommand,
                EndGameRuleCompletions);

            // Clear game rules command.
            _consoleHost.RegisterCommand("cleargamerules",
                string.Empty,
                "cleargamerules",
                ClearGameRulesCommand);
        }

        private void ShutdownGameRules()
        {
            _consoleHost.UnregisterCommand("addgamerule");
            _consoleHost.UnregisterCommand("endgamerule");
            _consoleHost.UnregisterCommand("cleargamerules");
        }

        /// <summary>
        ///     Game rules can be 'started' separately from being added. 'Starting' them usually
        ///     happens at round start while they can be added and removed before then.
        /// </summary>
        public void StartGameRule(GameRulePrototype rule)
        {
            if (!IsGameRuleAdded(rule))
                AddGameRule(rule);

            _allPreviousGameRules.Add((RoundDuration(), rule));
            _sawmill.Info($"Started game rule {rule.ID}");

            if (_startedGameRules.Add(rule))
                RaiseLocalEvent(new GameRuleStartedEvent(rule));
        }

        /// <summary>
        ///     Ends a game rule.
        ///     This always includes removing it (from added game rules) so that behavior
        ///     is not separate from this.
        /// </summary>
        /// <param name="rule"></param>
        public void EndGameRule(GameRulePrototype rule)
        {
            if (!IsGameRuleAdded(rule))
                return;

            _addedGameRules.Remove(rule);
            _sawmill.Info($"Ended game rule {rule.ID}");

            if (IsGameRuleStarted(rule))
                _startedGameRules.Remove(rule);
            RaiseLocalEvent(new GameRuleEndedEvent(rule));
        }

        /// <summary>
        ///     Adds a game rule to the list, but does not
        ///     start it yet, instead waiting until the rule is actually started by other code (usually roundstart)
        /// </summary>
        public bool AddGameRule(GameRulePrototype rule)
        {
            if (!_addedGameRules.Add(rule))
                return false;

            _sawmill.Info($"Added game rule {rule.ID}");
            RaiseLocalEvent(new GameRuleAddedEvent(rule));
            return true;
        }

        public bool IsGameRuleAdded(GameRulePrototype rule)
        {
            return _addedGameRules.Contains(rule);
        }

        public bool IsGameRuleAdded(string rule)
        {
            foreach (var ruleProto in _addedGameRules)
            {
                if (ruleProto.ID.Equals(rule))
                    return true;
            }

            return false;
        }

        public bool IsGameRuleStarted(GameRulePrototype rule)
        {
            return _startedGameRules.Contains(rule);
        }

        public bool IsGameRuleStarted(string rule)
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

        #region Command Implementations

        [AdminCommand(AdminFlags.Fun)]
        private void AddGameRuleCommand(IConsoleShell shell, string argstr, string[] args)
        {
            if (args.Length == 0)
                return;

            foreach (var ruleId in args)
            {
                if (!_prototypeManager.TryIndex<GameRulePrototype>(ruleId, out var rule))
                    continue;

                AddGameRule(rule);

                // Start rule if we're already in the middle of a round
                if(RunLevel == GameRunLevel.InRound)
                    StartGameRule(rule);
            }
        }

        private CompletionResult AddGameRuleCompletions(IConsoleShell shell, string[] args)
        {
            var activeIds = _addedGameRules.Select(c => c.ID);
            return CompletionResult.FromHintOptions(CompletionHelper.PrototypeIDs<GameRulePrototype>().Where(p => !activeIds.Contains(p.Value)),
                "<rule>");
        }

        [AdminCommand(AdminFlags.Fun)]
        private void EndGameRuleCommand(IConsoleShell shell, string argstr, string[] args)
        {
            if (args.Length == 0)
                return;

            foreach (var ruleId in args)
            {
                if (!_prototypeManager.TryIndex<GameRulePrototype>(ruleId, out var rule))
                    continue;

                EndGameRule(rule);
            }
        }

        private CompletionResult EndGameRuleCompletions(IConsoleShell shell, string[] args)
        {
            return CompletionResult.FromHintOptions(_addedGameRules.Select(c => new CompletionOption(c.ID)),
                "<added rule>");
        }

        [AdminCommand(AdminFlags.Fun)]
        private void ClearGameRulesCommand(IConsoleShell shell, string argstr, string[] args)
        {
            ClearGameRules();
        }

        #endregion
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
