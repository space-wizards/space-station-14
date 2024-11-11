using System.Reflection;
using Content.Shared.Guidebook;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Guidebook;

/// <summary>
/// Server system for identifying component fields/properties to extract values from entity prototypes.
/// Extracted data is sent to clients when they connect or when prototypes are reloaded.
/// </summary>
public sealed class GuidebookDataSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;

    private readonly Dictionary<string, List<MemberInfo>> _tagged = [];
    private GuidebookData _cachedData = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<RequestGuidebookDataEvent>(OnRequestRules);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        // Build initial cache
        GatherData(ref _cachedData);
    }

    private void OnRequestRules(RequestGuidebookDataEvent ev, EntitySessionEventArgs args)
    {
        // Send cached data to requesting client
        var sendEv = new UpdateGuidebookDataEvent(_cachedData);
        RaiseNetworkEvent(sendEv, args.SenderSession);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        // We only care about entity prototypes
        if (!args.WasModified<EntityPrototype>())
            return;

        // The entity prototypes changed! Clear our cache and regather data
        RebuildDataCache();

        // Send new data to all clients
        var ev = new UpdateGuidebookDataEvent(_cachedData);
        RaiseNetworkEvent(ev);
    }

    private void GatherData(ref GuidebookData cache)
    {
        // Just for debug metrics
        var memberCount = 0;
        var prototypeCount = 0;

        if (_tagged.Count == 0)
        {
            // Scan component registrations to find members tagged for extraction
            foreach (var registration in EntityManager.ComponentFactory.GetAllRegistrations())
            {
                foreach (var member in registration.Type.GetMembers())
                {
                    if (member.HasCustomAttribute<GuidebookDataAttribute>())
                    {
                        // Note this component-member pair for later
                        _tagged.GetOrNew(registration.Name).Add(member);
                        memberCount++;
                    }
                }
            }
        }

        // Scan entity prototypes for the component-member pairs we noted
        var entityPrototypes = _protoMan.EnumeratePrototypes<EntityPrototype>();
        foreach (var prototype in entityPrototypes)
        {
            foreach (var (component, entry) in prototype.Components)
            {
                if (!_tagged.TryGetValue(component, out var members))
                    continue;

                prototypeCount++;

                foreach (var member in members)
                {
                    // It's dumb that we can't just do member.GetValue, but we can't, so
                    var value = member switch
                    {
                        FieldInfo field => field.GetValue(entry.Component),
                        PropertyInfo property => property.GetValue(entry.Component),
                        _ => throw new NotImplementedException("Unsupported member type")
                    };
                    // Add it into the data cache
                    cache.AddData(prototype.ID, component, member.Name, value);
                }
            }
        }

        Log.Debug($"Collected {cache.Count} Guidebook Protodata value(s) - {prototypeCount} matched prototype(s), {_tagged.Count} component(s), {memberCount} member(s)");
    }

    /// <summary>
    /// Clears the cached data, then regathers it.
    /// </summary>
    private void RebuildDataCache()
    {
        _cachedData.Clear();
        GatherData(ref _cachedData);
    }
}
