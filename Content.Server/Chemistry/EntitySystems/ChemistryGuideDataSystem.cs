using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Reaction;
using Content.Server.Chemistry.ReactionEffects;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.Chemistry.EntitySystems;


public sealed class ChemistryGuideDataSystem : SharedChemistryGuideDataSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        PrototypeManager.PrototypesReloaded += PrototypeManagerReload;

        _player.PlayerStatusChanged += OnPlayerStatusChanged;

        InitializeServerRegistry();
    }

    private void InitializeServerRegistry()
    {
        var changeset = new ReagentGuideChangeset(new Dictionary<string, ReagentGuideEntry>(), new HashSet<string>(), new Dictionary<string, Dictionary<string, uint>>(), new HashSet<string>());
        foreach (var proto in PrototypeManager.EnumeratePrototypes<ReagentPrototype>())
        {
            var entry = new ReagentGuideEntry(proto, PrototypeManager, EntityManager.EntitySysManager);
            changeset.ReagentEffectEntries.Add(proto.ID, entry);
            ReagentRegistry[proto.ID] = entry;
        }

        foreach (var proto in PrototypeManager.EnumeratePrototypes<ReactionPrototype>())
        {
            foreach (CreateEntityReactionEffect effect in proto.Effects.OfType<CreateEntityReactionEffect>())
            {
                if (!changeset.ReactionSolidProductEntries.ContainsKey(proto.ID))
                {
                    changeset.ReactionSolidProductEntries.Add(proto.ID, new Dictionary<string, uint>());
                    ReactionRegistry.Add(proto.ID, new Dictionary<string, uint>());
                }
                changeset.ReactionSolidProductEntries[proto.ID].Add(effect.Entity, effect.Number);
                ReactionRegistry[proto.ID].Add(effect.Entity, effect.Number);
            }
        }

        var ev = new ReagentGuideRegistryChangedEvent(changeset);
        RaiseNetworkEvent(ev);
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus != SessionStatus.Connected)
            return;
        var sendEv = new ReagentGuideRegistryChangedEvent(new ReagentGuideChangeset(ReagentRegistry, new HashSet<string>(), ReactionRegistry, new HashSet<string>()));
        RaiseNetworkEvent(sendEv, e.Session);
    }

    private void PrototypeManagerReload(PrototypesReloadedEventArgs obj)
    {
        if (!obj.ByType.TryGetValue(typeof(ReagentPrototype), out var reagents))
            return;

        var changeset = new ReagentGuideChangeset(new Dictionary<string, ReagentGuideEntry>(), new HashSet<string>(), new Dictionary<string, Dictionary<string, uint>>(), new HashSet<string>());

        foreach (var (id, proto) in reagents.Modified)
        {
            var reagentProto = (ReagentPrototype) proto;
            var entry = new ReagentGuideEntry(reagentProto, PrototypeManager, EntityManager.EntitySysManager);
            changeset.ReagentEffectEntries.Add(id, entry);
            ReagentRegistry[id] = entry;
        }

        var ev = new ReagentGuideRegistryChangedEvent(changeset);
        RaiseNetworkEvent(ev);
    }
}
