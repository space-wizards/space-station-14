using Content.Shared.EntityFlags.Components;
using Content.Shared.EntityFlags.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityFlags.Systems;

public sealed class EntityFlagSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    private Dictionary<string, FlagData> _cachedFlags = new();
    private Dictionary<string, byte> _cachedEntityFlagGroupIds = new();
    private const string DefaultGroupIDName = "default";

    public override void Initialize()
    {
        CacheFlags();
        _prototypeManager.PrototypesReloaded += _ => CacheFlags();

        SubscribeLocalEvent<EntityFlagComponent, ComponentGetState>(OnEntityFlagGetState);
        SubscribeLocalEvent<EntityFlagComponent, ComponentHandleState>(OnEntityFlagHandleState);
    }

    private void OnEntityFlagHandleState(EntityUid uid, EntityFlagComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not EntityFlagComponentState state)
            return;
        component.Flags = state.Flags;
    }

    private void OnEntityFlagGetState(EntityUid uid, EntityFlagComponent component, ref ComponentGetState args)
    {
        args.State = new EntityFlagComponentState(
            component.Flags
        );
    }

    private byte GetIdForFlagGroup(string? flagGroupName)
    {
        if (flagGroupName == null)
            return 0;
        if (_cachedEntityFlagGroupIds.TryGetValue(flagGroupName, out var groupId))
            return groupId;
        Logger.Warning("Could not find group id for flag: " + flagGroupName + " using default GroupId!");
        return 0;
    }

    public byte GetFlagGroupId(string flagGroupName)
    {
        return GetIdForFlagGroup(flagGroupName);
    }

    private void CacheFlags()
    {
        _cachedFlags.Clear();
        _cachedEntityFlagGroupIds.Clear();
        _cachedEntityFlagGroupIds.Add(DefaultGroupIDName, 0);
        foreach (var flagPrototype in _prototypeManager.EnumeratePrototypes<EntityFlagGroupPrototype>())
        {
            if (flagPrototype.GroupId == 0)
            {
                Logger.Error(flagPrototype.ID + ": GroupFlagId 0 is reserved for default!");
                continue;
            }

            if (!_cachedEntityFlagGroupIds.TryAdd(flagPrototype.ID, flagPrototype.GroupId))
            {
                Logger.Error(flagPrototype.ID + ": Duplicate flag group found for ID: " + flagPrototype.GroupId);
            }
        }

        foreach (var flagPrototype in _prototypeManager.EnumeratePrototypes<EntityFlagPrototype>())
        {
            _cachedFlags.Add(flagPrototype.ID,
                new FlagData(GetIdForFlagGroup(flagPrototype.FlagGroup), flagPrototype.Local));
        }
    }

    public override void Shutdown()
    {
        _cachedFlags.Clear();
        _cachedEntityFlagGroupIds.Clear();
    }

    public bool IsLocalFlag(string flagName)
    {
        return _cachedFlags.TryGetValue(flagName, out var flagData) && flagData.IsLocal;
    }

    public bool AddFlag(EntityUid entityUid, string flagName)
    {
        if (!_cachedFlags.TryGetValue(flagName, out var flagData))
        {
            Logger.Error("Flag: " + flagName + " not found!");
            return false;
        }

        if (flagData.IsLocal)
            return AddFlagToEntity(entityUid, flagName, flagData);
        Logger.Error("Flag: " + flagName + " is not networked, use AddFlagLocal instead!");
        return false;
    }

    public bool AddFlagLocal(EntityUid entityUid, string flagName)
    {
        if (!_cachedFlags.TryGetValue(flagName, out var flagData))
        {
            Logger.Error("Flag: " + flagName + " not found!");
            return false;
        }

        if (!flagData.IsLocal)
            return AddFlagToEntity(entityUid, flagName, flagData);
        Logger.Error("Flag: " + flagName + " is a networked flag, use AddFlag instead!");
        return false;
    }

    public bool RemoveFlag(EntityUid entityUid, string flagName)
    {
        if (!_cachedFlags.TryGetValue(flagName, out var flagData))
        {
            Logger.Error("Flag: " + flagName + " not found!");
            return false;
        }

        if (flagData.IsLocal)
            return RemoveFlagFromEntity(entityUid, flagName, flagData);
        Logger.Error("Flag: " + flagName + " is not networked, use RemoveFlagLocal instead!");
        return false;
    }

    public bool RemoveFlagLocal(EntityUid entityUid, string flagName)
    {
        if (!_cachedFlags.TryGetValue(flagName, out var flagData))
        {
            Logger.Error("Flag: " + flagName + " not found!");
            return false;
        }

        if (!flagData.IsLocal)
            return RemoveFlagFromEntity(entityUid, flagName, flagData);
        Logger.Error("Flag: " + flagName + " is a networked flag, use RemoveFlag instead!");
        return false;
    }

    private bool AddFlagToEntity(EntityUid entityUid, string flagName, FlagData flagData)
    {
        var flagComp = EnsureComp<EntityFlagComponent>(entityUid);
        if (!flagComp.Flags.Add(flagName))
        {
            Logger.Warning("Flag: " + flagName + " is already present!");
            return false;
        }

        var ev = new EntityFlagAddedEvent(flagData.GroupId, flagName);
        RaiseLocalEvent(entityUid, ev);
        Dirty(entityUid);
        return true;
    }

    private bool RemoveFlagFromEntity(EntityUid entityUid, string flagName, FlagData flagData)
    {
        if (!TryComp<EntityFlagComponent>(entityUid, out var entityFlagComponent))
            return false;

        if (!entityFlagComponent.Flags.Remove(flagName))
        {
            return false;
        }

        var ev = new EntityFlagRemovedEvent(flagData.GroupId, flagName);
        RaiseLocalEvent(entityUid, ev);
        Dirty(entityUid);
        return true;
    }

    private record struct FlagData(byte GroupId, bool IsLocal);
}
