using Content.Shared.Atmos;

namespace Content.Shared.NodeContainer.NodeGroups;

public interface IPipeNet : INodeGroup, IGasMixtureHolder
{
    /// <summary>
    /// Causes gas in the PipeNet to react.
    /// </summary>
    void Update();
}
