using Content.Shared.Atmos;
using Content.Shared.Disposal.Holder;
using Content.Shared.Disposal.Tube;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared.Disposal.Unit;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(new[] { typeof(SharedDisposalTubeSystem), typeof(SharedDisposalHolderSystem) })]
public sealed partial class DisposalHolderComponent : Component, IGasMixtureHolder
{
    [DataField]
    public Container? Container;

    /// <summary>
    ///     Sets how many seconds it takes to traverse one pipe length
    /// </summary>
    [DataField]
    public float TraversalSpeed { get; set; } = 5f;

    [DataField, AutoNetworkedField]
    public EntityUid? CurrentTube { get; set; }

    [DataField, AutoNetworkedField]
    public EntityUid? NextTube { get; set; }

    // CurrentDirection is not null when CurrentTube isn't null.
    [DataField, AutoNetworkedField]
    public Direction CurrentDirection { get; set; } = Direction.Invalid;

    /// <summary>Mistake prevention</summary>
    [DataField, AutoNetworkedField]
    public bool IsExitingDisposals { get; set; } = false;

    /// <summary>
    ///     A list of tags attached to the content, used for sorting
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<string> Tags { get; set; } = new();

    [DataField, AutoNetworkedField]
    public GasMixture Air { get; set; } = new(70);
}
