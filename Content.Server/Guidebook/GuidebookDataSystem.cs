using System.Linq;
using System.Reflection;
using Content.Shared.Guidebook;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Utility;

namespace Content.Server.Guidebook;

/// <summary>
/// Server system for identifying component fields/properties to extract values from entity prototypes.
/// Extracted data is sent to clients when they connect or when prototypes are reloaded.
/// </summary>
public sealed partial class GuidebookDataSystem : EntitySystem
{
    [Dependency] private IReflectionManager _reflection = default!;

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
        var components = _reflection
            .GetAllChildren<IGuidebookData>()
            .Select(t => EntityManager.ComponentFactory.GetComponentName(t))
            .ToHashSet();

        // Scan component registrations to find members tagged for extraction
        var entityPrototypes = ProtoMan.EnumeratePrototypes<EntityPrototype>();
        foreach (var prototype in entityPrototypes)
        {
            foreach (var (component, entry) in prototype.Components)
            {
                if (!components.Contains(component))
                    continue;

                prototypeCount++;

                var data = (IGuidebookData) entry.Component;
                foreach (var name in data.GetFieldNames())
                {
                    // Add it into the data cache
                    var value = data.GetFieldValue(name);
                    cache.AddData(prototype.ID, component, name, value);
                    memberCount++;
                }
            }
        }

        Log.Debug($"Collected {cache.Count} Guidebook Protodata value(s) - {prototypeCount} matched prototype(s), {components.Count} component(s), {memberCount} member(s)");
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
