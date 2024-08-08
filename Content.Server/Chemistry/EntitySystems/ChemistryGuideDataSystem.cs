using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reagent;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.EntitySystems;


public sealed class ChemistryGuideDataSystem : SharedChemistryGuideDataSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(PrototypeManagerReload);
        _player.PlayerStatusChanged += OnPlayerStatusChanged;

        InitializeServerRegistry();
    }

    private void InitializeServerRegistry()
    {
        var changeset = new ReagentGuideChangeset(new Dictionary<string, ReagentGuideEntry>(), new HashSet<string>());
        foreach (var (reagentDef,_) in ChemistryRegistry.EnumeratePrototypes())
        {
            var entry = new ReagentGuideEntry(reagentDef, PrototypeManager, EntityManager.EntitySysManager);
            changeset.GuideEntries.Add(reagentDef.Id, entry);
            Registry[reagentDef.Id] = entry;
        }

        var ev = new ReagentGuideRegistryChangedEvent(changeset);
        RaiseNetworkEvent(ev);
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus != SessionStatus.Connected)
            return;

        var sendEv = new ReagentGuideRegistryChangedEvent(new ReagentGuideChangeset(Registry, new HashSet<string>()));
        RaiseNetworkEvent(sendEv, e.Session);
    }

    private void PrototypeManagerReload(PrototypesReloadedEventArgs obj)//TODO: hook chemregistry reload instead
    {
        if (!obj.ByType.TryGetValue(typeof(ReagentPrototype), out var reagents))
            return;

        var changeset = new ReagentGuideChangeset(new Dictionary<string, ReagentGuideEntry>(), new HashSet<string>());

        foreach (var (id, proto) in reagents.Modified)
        {
            var reagentProto = (ReagentPrototype) proto;
            if (!ChemistryRegistry.TryIndexPrototype(reagentProto.ID, out var reagentDef))
            {
                Log.Error($"{reagentProto.ID} could not be found in the reagent registry!");
                continue;
            }
            var entry = new ReagentGuideEntry(reagentDef.ReagentDefinition, PrototypeManager, EntityManager.EntitySysManager);
            changeset.GuideEntries.Add(id, entry);
            Registry[id] = entry;
        }

        var ev = new ReagentGuideRegistryChangedEvent(changeset);
        RaiseNetworkEvent(ev);
    }
}
