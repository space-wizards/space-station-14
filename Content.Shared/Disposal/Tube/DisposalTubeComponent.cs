using Content.Shared.Damage;
using Content.Shared.Disposal.Unit;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Disposal.Tube;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedDisposalTubeSystem), typeof(SharedDisposableSystem))]
public sealed partial class DisposalTubeComponent : Component
{
    [DataField]
    public string ContainerId = "DisposalTube";

    [DataField, AutoNetworkedField]
    public bool Connected;

    [DataField]
    public SoundSpecifier ClangSound = new SoundPathSpecifier("/Audio/Effects/clang.ogg", AudioParams.Default.WithVolume(-5f));

    /// <summary>
    /// Container of entities that are currently inside this tube.
    /// </summary>
    [DataField]
    public DisposalHolderComponent? Contents;

    /// <summary>
    /// Damage dealt to containing entities on every turn.
    /// </summary>
    [DataField]
    public DamageSpecifier DamageOnTurn = new()
    {
        DamageDict = new()
        {
            { "Blunt", 0.0 },
        }
    };

    [DataField]
    public DisposalTubeType DisposalTubeType = DisposalTubeType.Disposals;
}

[ByRefEvent]
public record struct GetDisposalsConnectableDirectionsEvent
{
    public Direction[] Connectable;
}

[ByRefEvent]
public record struct GetDisposalsNextDirectionEvent(DisposalHolderComponent Holder)
{
    public Direction Next;
}

[Serializable, NetSerializable]
public enum DisposalTubeType
{
    Disposals,
    Transit
}

[Serializable, NetSerializable]
public enum DisposalTubeVisuals
{
    VisualState
}

[Serializable, NetSerializable]
public enum DisposalTubeVisualState
{
    Free = 0,
    Anchored,
}
