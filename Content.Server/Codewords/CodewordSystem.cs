using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.GameTicking.Events;
using Content.Shared.Database;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Codewords;

/// <summary>
/// Gamerule that provides codewords for other gamerules that rely on them.
/// </summary>
public sealed class CodewordSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
    }

    private void OnRoundStart(RoundStartingEvent ev)
    {
        var manager = Spawn();
        AddComp<CodewordManagerComponent>(manager);
    }

    /// <summary>
    /// Retrieves codewords for the faction specified.
    /// </summary>
    public string[] GetCodewords(ProtoId<CodewordFactionPrototype> faction)
    {
        var query = EntityQueryEnumerator<CodewordManagerComponent>();
        while (query.MoveNext(out  _, out var manager))
        {
            if (!manager.Codewords.TryGetValue(faction, out var codewordEntity))
                return GenerateForFaction(faction, ref manager);

            return Comp<CodewordComponent>(codewordEntity).Codewords;
        }

        Log.Warning("Codeword system not initialized. Returning empty array.");
        // While throwing in this situation would be cool, that causes a test fail (in SpawnAndDeleteEntityCountTest)
        // as the traitor codewords paper gets spawned in and calls this method,
        // but the "start round" event never gets called in this test case.
        return [];
    }

    private string[] GenerateForFaction(ProtoId<CodewordFactionPrototype> faction, ref CodewordManagerComponent manager)
    {
        var factionProto = _prototypeManager.Index<CodewordFactionPrototype>(faction.Id);

        var codewords = GenerateCodewords(factionProto.Generator);
        var codewordsContainer = EntityManager.Spawn(protoName:null, MapCoordinates.Nullspace);
        EnsureComp<CodewordComponent>(codewordsContainer)
            .Codewords = codewords;
        manager.Codewords[faction] = codewordsContainer;
        _adminLogger.Add(LogType.EventStarted, LogImpact.Low, $"Codewords generated for faction {faction}: {string.Join(", ", codewords)}");

        return codewords;
    }

    /// <summary>
    /// Generates codewords as specified by the <see cref="CodewordGeneratorPrototype"/> codeword generator.
    /// </summary>
    public string[] GenerateCodewords(ProtoId<CodewordGeneratorPrototype> generatorId)
    {
        var generator = _prototypeManager.Index(generatorId);

        var codewordPool = new List<string>();
        foreach (var dataset in generator.Words
                     .Select(datasetPrototype => _prototypeManager.Index(datasetPrototype)))
        {
            codewordPool.AddRange(dataset.Values);
        }

        var finalCodewordCount = Math.Min(generator.Amount, codewordPool.Count);
        var codewords = new string[finalCodewordCount];
        for (var i = 0; i < finalCodewordCount; i++)
        {
            codewords[i] = Loc.GetString(_random.PickAndTake(codewordPool));
        }
        return codewords;
    }
}
