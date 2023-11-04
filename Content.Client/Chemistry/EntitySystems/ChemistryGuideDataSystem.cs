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
        foreach (var remove in data.Removed)
        {
            Registry.Remove(remove);
        }

        foreach (var (key, val) in data.GuideEntries)
        {
            Registry[key] = val;
        }
    }
}
