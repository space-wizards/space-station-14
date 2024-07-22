using System.Collections.Frozen;
using System.Numerics;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.GameTicking;
using Content.Shared.Prototypes;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry.Systems;

public sealed partial class ChemistryRegistrySystem : EntitySystem
{
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;

    private const float DefaultMolarMass = 18;

    private FrozenDictionary<string, EntityUid> _reagentLookup =
        FrozenDictionary<string, EntityUid>.Empty;
    private Dictionary<string, EntityUid> _reagents = new ();
    private FrozenDictionary<string, EntityUid> _reactionLookup =
        FrozenDictionary<string, EntityUid>.Empty;
    private Dictionary<string, EntityUid> _reactions = new ();
    public override void Initialize()
    {
        _protoManager.PrototypesReloaded += ReloadData;

        if (_netManager.IsClient)
        {
            _playerManager.PlayerStatusChanged += PlayerManagerOnPlayerStatusChanged;
            SubscribeNetworkEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        }
        else
        {
            SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        }
    }

    private void PlayerManagerOnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        Log.Info("Loading Registry...");
        LoadData();
    }

    private void ReloadData(PrototypesReloadedEventArgs? obj = null)
    {
        Log.Info("Reloading Registry...");
        ClearData();
        LoadData();
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        Log.Info("Loading Registry...");
        ClearData();
        LoadData();
    }

    private void ClearData()
    {
        foreach (var (_, ent) in _reagentLookup)
        {
            Del(ent);
        }
        _reagentLookup = FrozenDictionary<string, EntityUid>.Empty;
        foreach (var (_, ent) in _reagents)
        {
            Del(ent);
        }
        _reagents.Clear();

        //Reactions

        foreach (var (_, ent) in _reactionLookup)
        {
            Del(ent);
        }
        _reactionLookup = FrozenDictionary<string, EntityUid>.Empty;
        foreach (var (_, ent) in _reactions)
        {
            Del(ent);
        }
        _reactions.Clear();
    }

    private void LoadData()
    {
        if (_reagentLookup.Count > 0)
        {
            Log.Error("Tried to load reagent definitions without clearing first!");
            return;
        }
        Log.Info("Loading Legacy Prototypes...");
        ConvertLegacyReagentPrototypes(ref _reagents);
        ConvertLegacyReactionPrototypes(ref _reactions);
        Log.Info("Done");
        foreach (var entProto in _protoManager.EnumeratePrototypes<EntityPrototype>())
        {
            if (LoadReagentData(entProto, ref _reagents))
                continue;
            LoadReactionData(entProto, ref _reactions);
        }
        SyncLookup();
        Log.Info($"{_reagents.Count} reagents registered");
        Log.Info($"{_reactions.Count} reactions registered");
        Log.Info($"Registry load complete!");
    }

    private void SyncLookup()
    {
        _reagentLookup = _reagents.ToFrozenDictionary();
        _reactionLookup = _reactions.ToFrozenDictionary();
    }

    private bool LoadReagentData(
        EntityPrototype entProto,
        ref Dictionary<string, EntityUid> pendingReagents)
    {
        if (!entProto.HasComponent<ReagentDefinitionComponent>(_compFactory))
            return false;
        var newEnt = Spawn(entProto.ID);
        pendingReagents.Add(
            entProto.ID,
            newEnt);
        return true;
    }

    private bool LoadReactionData(
        EntityPrototype entProto,
        ref Dictionary<string, EntityUid> pendingReactions)
    {
        if (!entProto.HasComponent<ReactionDefinitionComponent>(_compFactory))
            return false;
        var newEnt = Spawn(entProto.ID);
        pendingReactions.Add(
            entProto.ID,
            newEnt);
        return true;
    }

    public override void Shutdown()
    {
        ClearData();
    }

    public bool RegisterReagent(
        string reagentName,
        ReagentDefinitionComponent reagentDefinition,
        ReagentMetamorphicSpriteComponent? metaSprite = null)
    {
        if (_reagentLookup.ContainsKey(reagentName))
        {
            Log.Error($"{reagentName} is already defined!");
            return false;
        }
        var newEnt = Spawn();
        AddComp(newEnt, reagentDefinition);
        if (metaSprite != null)
            AddComp(newEnt, metaSprite);
        _reagents.Add(reagentName, newEnt);
        SyncLookup();
        return true;
    }

    public bool RemoveReagent(string reagentName)
    {
        if (!_reagents.Remove(reagentName))
        {
            Log.Warning($"{reagentName} could not be found in reagent registry!");
            return false;
        }
        SyncLookup();
        return true;
    }

    public bool RegisterReaction(
        string reagentName,
        ReactionDefinitionComponent reactionDefinition,
        Vector2? temperatureRange = null,
        List<ProtoId<MixingCategoryPrototype>>? mixingCategories = null)
    {
        if (_reagentLookup.ContainsKey(reagentName))
        {
            Log.Error($"{reagentName} is already defined!");
            return false;
        }
        var newEnt = Spawn();
        AddComp(newEnt, reactionDefinition);
        if (temperatureRange != null)
        {
            AddComp(newEnt,
                new RequiresReactionTemperatureComponent
                {
                    MinimumTemperature = temperatureRange.Value.X,
                    MaximumTemperature = temperatureRange.Value.Y,
                });
        }

        if (mixingCategories != null)
        {
            AddComp(newEnt,
                new RequiresReactionMixingComponent {MixingCategories = mixingCategories});
        }

        _reagents.Add(reagentName, newEnt);
        SyncLookup();
        return true;
    }

    public bool RemoveReaction(string reactionName)
    {
        if (!_reactions.Remove(reactionName))
        {
            Log.Warning($"{reactionName} could not be found in reactions registry!");
            return false;
        }
        SyncLookup();
        return true;
    }

    public Entity<ReagentDefinitionComponent> GetReagentEntity(string reagentName)
    {
        var ent = _reagents[reagentName];
        return (ent, Comp<ReagentDefinitionComponent>(ent));
    }

    public Entity<ReagentDefinitionComponent> GetReagentEntity(ReagentId reagentId)
    {
        return GetReagentEntity(reagentId.Prototype);
    }

    public ReagentDefinitionComponent GetReagent(string reagentName)
    {
        return GetReagentEntity(reagentName);
    }

    public ReagentDefinitionComponent GetReagent(ReagentId reagentId)
    {
        return GetReagent(reagentId.Prototype);
    }
}
