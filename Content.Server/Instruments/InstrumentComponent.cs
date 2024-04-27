using Content.Server.UserInterface;
using Content.Shared.Instruments;
using Robust.Shared.Player;
using ActivatableUIComponent = Content.Shared.UserInterface.ActivatableUIComponent;

namespace Content.Server.Instruments;

[RegisterComponent]
public sealed partial class InstrumentComponent : SharedInstrumentComponent
{
    [Dependency] private readonly IEntityManager _entMan = default!;

    [ViewVariables] public float Timer = 0f;
    [ViewVariables] public int BatchesDropped = 0;
    [ViewVariables] public int LaggedBatches = 0;
    [ViewVariables] public int MidiEventCount = 0;
    [ViewVariables] public uint LastSequencerTick = 0;

    // TODO Instruments: Make this ECS
    public EntityUid? InstrumentPlayer =>
        _entMan.GetComponentOrNull<ActivatableUIComponent>(Owner)?.CurrentSingleUser
        ?? _entMan.GetComponentOrNull<ActorComponent>(Owner)?.PlayerSession.AttachedEntity;
}

[RegisterComponent]
public sealed partial class ActiveInstrumentComponent : Component
{
}
