using Content.Shared.Chemistry;

namespace Content.Client.Chemistry.EntitySystems;

/// <inheritdoc/>
public sealed class ChemistryGuideDataSystem : SharedChemistryGuideDataSystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        Net.RegisterNetMessage<MsgUpdateReagentGuideRegistry>(OnReceiveRegistryUpdate);
    }

    private void OnReceiveRegistryUpdate(MsgUpdateReagentGuideRegistry message)
    {
        Logger.Debug("received changes");
        var data = message.Changeset;

        foreach (var remove in data.Removed)
        {
            _reagentGuideRegistry.Remove(remove);
        }

        foreach (var (key, val) in data.GuideEntries)
        {
            _reagentGuideRegistry[val.ReagentPrototype] = val;
        }
    }
}
