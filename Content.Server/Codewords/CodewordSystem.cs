using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.GameTicking.Rules;
using Content.Shared.Database;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Codewords;

/// <summary>
/// Gamerule that provides codewords for other gamerules that rely on them.
/// </summary>
public sealed class CodewordSystem : GameRuleSystem<CodewordRuleComponent>
{
    [ValidatePrototypeId<EntityPrototype>]
    public static string RuleComponent = "CodewordRule";

    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;

    /// <summary>
    /// Ensures codewords are available.
    /// </summary>
    public void EnsureAvailable()
    {
        if (CheckCodewordsAvailable())
            return;

        // We already have codewords, no need to do anything.
        GameTicker.StartGameRule(RuleComponent);
    }

    /// <summary>
    /// Checks if the codeword system has any valid codewords.
    /// </summary>
    /// <returns>True if there is a valid codeword gamerule. False if there is none.</returns>
    public bool CheckCodewordsAvailable()
    {
        var query = EntityQueryEnumerator<CodewordRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out _, out var gameRuleComponent))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRuleComponent))
                continue;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Retrieves codewords for the faction specified.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no codewords have been generated for that faction.</exception>
    public string[] GetCodewords(ProtoId<CodewordFaction> faction)
    {
        var query = EntityQueryEnumerator<CodewordRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var codewordRuleComponent, out var gameRuleComponent))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRuleComponent))
                continue;

            if (!codewordRuleComponent.Codewords.TryGetValue(faction, out var codewordEntity))
                continue;

            return Comp<CodewordComponent>(codewordEntity).Codewords;
        }

        throw new InvalidOperationException($"Tried to index codewords for faction {faction}, but no codewords were generated for that faction.");
    }

    protected override void Added(EntityUid uid, CodewordRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);

        foreach (var (id, generatorId) in component.Generators)
        {
            var codewords = GenerateCodewords(generatorId);
            var codewordsContainer = EntityManager.Spawn(protoName:null, MapCoordinates.Nullspace);
            EnsureComp<CodewordComponent>(codewordsContainer)
                .Codewords = codewords;
            component.Codewords[id] = codewordsContainer;
            _adminLogger.Add(LogType.EventStarted, LogImpact.Low, $"Codewords generated for faction {id}: {string.Join(", ", codewords)}");
        }
    }

    /// <summary>
    /// Generates codewords as specified by the <see cref="CodewordGenerator"/> codeword generator.
    /// </summary>
    public string[] GenerateCodewords(ProtoId<CodewordGenerator> generatorId)
    {
        var generator = _prototypeManager.Index(generatorId);

        var adjectives = _prototypeManager.Index(generator.CodewordAdjectives).Values;
        var verbs = _prototypeManager.Index(generator.CodewordVerbs).Values;
        var codewordPool = adjectives.Concat(verbs).ToList();
        var finalCodewordCount = Math.Min(generator.Amount, codewordPool.Count);
        var codewords = new string[finalCodewordCount];
        for (var i = 0; i < finalCodewordCount; i++)
        {
            codewords[i] = Loc.GetString(RobustRandom.PickAndTake(codewordPool));
        }
        return codewords;
    }
}
