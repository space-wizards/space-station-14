using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.GameTicking.Events;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using static System.Linq.Enumerable;

namespace Content.Shared.Codewords;

/// <summary>
/// System that generates and handles the assignment of codewords.
/// </summary>
public sealed partial class CodewordSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedMindSystem _mind = default!;

    [Dependency] private EntityQuery<RoleCodewordComponent> _codewordQuery;

    [SubscribeLocalEvent]
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
        var factionProto = ProtoMan.Index<CodewordFactionPrototype>(faction.Id);

        var codewords = GenerateCodewords(factionProto.Generator);
        var codewordsContainer = Spawn(prototype: null, MapCoordinates.Nullspace);
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
        var generator = ProtoMan.Index(generatorId);

        var codewordPool = new List<string>();
        foreach (var dataset in generator.Words
                     .Select(datasetPrototype => ProtoMan.Index(datasetPrototype)))
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

    /// <summary>
    /// Returns all codewords a specific player knows
    /// </summary>
    /// <param name="player">Player account</param>
    /// <returns>All codewords known by this entity.</returns>
    [PublicAPI]
    public List<CodewordsData> GetPlayerCodewords(NetUserId? player)
    {
        return player != null && _mind.TryGetMind(player.Value, out var mind)
            ? GetMindCodewords(mind.Value.AsNullable())
            : new List<CodewordsData>();
    }

    /// <summary>
    /// Returns all codewords stored in a mind.
    /// </summary>
    /// <param name="entity">Entity</param>
    /// <returns>All codewords known by this entity.</returns>
    [PublicAPI]
    public List<CodewordsData> GetEntityCodewords(Entity<MindContainerComponent?> entity)
    {
        return _mind.TryGetMind(entity, out var uid, out var mind)
            ? GetMindCodewords((uid, mind))
            : new List<CodewordsData>();
    }

    /// <summary>
    /// Returns all codewords stored in a mind.
    /// </summary>
    /// <param name="mind">Mind entity</param>
    /// <returns>All codewords known by this mind.</returns>
    [PublicAPI]
    public List<CodewordsData> GetMindCodewords(Entity<MindComponent?> mind)
    {
        var data = new List<CodewordsData>();
        if (!Resolve(mind, ref mind.Comp))
            return data;

        foreach (var role in mind.Comp.MindRoleContainer.ContainedEntities)
        {
            if (GetRoleCodewords(role) is not { } codewords)
                continue;

            data.Add(codewords);
        }

        return data;
    }

    /// <summary>
    /// Returns codewords associated with this role
    /// </summary>
    /// <param name="role">Role entity</param>
    /// <returns>Codewords for this role, if they exist.</returns>
    [PublicAPI]
    public CodewordsData? GetRoleCodewords(Entity<RoleCodewordComponent?> role)
    {
        if (!_codewordQuery.Resolve(role, ref role.Comp, false))
            return null;

        return role.Comp.RoleCodewords;
    }

    /// <summary>
    /// Sets the codewords for a given mindrole entity.
    /// </summary>
    /// <param name="ent">Mindrole entity.</param>
    /// <param name="codewords">Codewords</param>
    /// <param name="color">Color the codewords show as</param>
    [PublicAPI]
    public void SetRoleCodewords(Entity<RoleCodewordComponent> ent, List<string> codewords, Color color)
    {
        var data = new CodewordsData(color, codewords);
        ent.Comp.RoleCodewords = data;
        Dirty(ent);
    }
}
