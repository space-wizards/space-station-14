using Content.Shared.TapeRecorder;
using Robust.Shared.Timing;

namespace Content.Client.TapeRecorder;

/// <summary>
/// Required for client side prediction stuff
/// </summary>
public sealed class TapeRecorderSystem : SharedTapeRecorderSystem
{
    [Dependency] protected readonly IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        base.Update(frameTime);
    }
}
