using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Chemistry;

/// <summary>
/// This handles the chemistry guidebook and caching it.
/// </summary>
public sealed class ChemistryGuideDataSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeNetworkEvent<UpdateReagentGuideEntry>(Handler);
    }

    private void Handler(UpdateReagentGuideEntry ev)
    {
        if (!_net.IsClient)
            return;
    }
}

[Serializable, NetSerializable]
public sealed class UpdateReagentGuideEntry : EntityEventArgs
{
    public string Reagent;
    public ReagentGuideEntry GuideEntry;

    public UpdateReagentGuideEntry(string reagent, ReagentGuideEntry guideEntry)
    {
        Reagent = reagent;
        GuideEntry = guideEntry;
    }
}
