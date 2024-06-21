using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Presets;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Random;
using Content.Shared.Database;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Configuration;
using Robust.Shared.Utility;
using Content.Shared.Storage;
using Microsoft.CodeAnalysis;

namespace Content.Server.GameTicking.Rules;

public sealed class RandomRuleSystem : GameRuleSystem<RandomRuleComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IComponentFactory _compFact = default!;

    private string _ruleCompName = default!;
    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void Added(EntityUid uid, RandomRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        var selectedRules = EntitySpawnCollection.GetSpawns(component.SelectableGameRules, _random);

        Random rnd = new Random();
        int ruleQuant = rnd.Next(component.MinRules, component.MaxRules);

        for (int i = 0; i < ruleQuant; i++)
        {
            string? slag = null;

            foreach (var rule in selectedRules)
            {
                if (selectedRules == null)
                    return;
                /// If the station is already initialized, just start the rule, otherwise let that happen at the start of round.
                if (GameTicker.RunLevel <= GameRunLevel.InRound)
                    GameTicker.AddGameRule(rule);
                else
                    GameTicker.StartGameRule(rule);
                slag = rule;
            }

            ///prevents same gamerule twice in one run.
            if (!string.IsNullOrEmpty(slag))
            {
                selectedRules.RemoveAt(selectedRules.IndexOf(slag));
            }
        }
    }
}
