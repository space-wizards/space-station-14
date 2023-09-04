#nullable enable
using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.UnitTesting;

namespace Content.IntegrationTests.Pair;

// This partial class contains helper methods to deal with yaml prototypes.
public sealed partial class TestPair
{
    private Dictionary<Type, HashSet<string>> _loadedPrototypes = new();
    private HashSet<string> _loadedEntityPrototypes = new();

    public async Task LoadPrototypes(List<string> prototypes)
    {
        await LoadPrototypes(Server, prototypes);
        await LoadPrototypes(Client, prototypes);
    }

    private async Task LoadPrototypes(RobustIntegrationTest.IntegrationInstance instance, List<string> prototypes)
    {
        var changed = new Dictionary<Type, HashSet<string>>();
        foreach (var file in prototypes)
        {
            instance.ProtoMan.LoadString(file, changed: changed);
        }

        await instance.WaitPost(() =>
        {
            instance.ProtoMan.ResolveResults();
            instance.ProtoMan.ReloadPrototypes(changed);
        });

        foreach (var (kind, ids) in changed)
        {
            _loadedPrototypes.GetOrNew(kind).UnionWith(ids);
        }

        if (_loadedPrototypes.TryGetValue(typeof(EntityPrototype), out var entIds))
            _loadedEntityPrototypes.UnionWith(entIds);
    }

    public bool IsTestPrototype(EntityPrototype proto)
    {
        return _loadedEntityPrototypes.Contains(proto.ID);
    }

    public bool IsTestEntityPrototype(string id)
    {
        return _loadedEntityPrototypes.Contains(id);
    }

    public bool IsTestPrototype<TPrototype>(string id) where TPrototype : IPrototype
    {
        return IsTestPrototype(typeof(TPrototype), id);
    }

    public bool IsTestPrototype<TPrototype>(TPrototype proto) where TPrototype : IPrototype
    {
        return IsTestPrototype(typeof(TPrototype), proto.ID);
    }

    public bool IsTestPrototype(Type kind, string id)
    {
        return _loadedPrototypes.TryGetValue(kind, out var ids) && ids.Contains(id);
    }
}