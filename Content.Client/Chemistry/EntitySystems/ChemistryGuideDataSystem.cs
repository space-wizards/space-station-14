using Content.Shared.Chemistry;

namespace Content.Client.Chemistry.EntitySystems;

/// <inheritdoc/>
public sealed class ChemistryGuideDataSystem : SharedChemistryGuideDataSystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ReagentGuideRegistryChangedEvent>(OnReceiveRegistryUpdate);
    }

    private void OnReceiveRegistryUpdate(ReagentGuideRegistryChangedEvent message)
    {
        var data = message.Changeset;
        foreach (var remove in data.ReagentEffectRemoved)
        {
            ReagentRegistry.Remove(remove);
        }

        foreach (var remove in data.ReactionSolidProductRemoved)
        {
            ReactionRegistry.Remove(remove);
        }

        foreach (var (key, val) in data.ReagentEffectEntries)
        {
            ReagentRegistry[key] = val;
        }

        Logger.Error(data.ReactionSolidProductEntries.Count.ToString());
        foreach (var (key, val) in data.ReactionSolidProductEntries)
        {
            ReactionRegistry[key] = val;
        }
    }
}
