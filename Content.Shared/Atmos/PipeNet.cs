using Content.Shared.NodeContainer.NodeGroups;

namespace Content.Shared.Atmos;

public sealed class PipeNet : BaseNodeGroup, IGasMixtureHolder
{
    [ViewVariables]
    public GasMixture Air { get; set; } = new() {Temperature = Atmospherics.T20C};

    [ViewVariables]
    public EntityUid? Grid;
}
