using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace Content.Client.Items.UI;

/// <summary>
/// A base for item status controls that poll data every frame. Avoids UI updates if data didn't change.
/// </summary>
/// <typeparam name="TData">The full status control data that is polled every frame.</typeparam>
public abstract class PollingItemStatusControl<TData> : Control where TData : struct, IEquatable<TData>
{
    private TData _lastData;

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        var newData = PollData();
        if (newData.Equals(_lastData))
            return;

        _lastData = newData;
        Update(newData);
    }

    protected abstract TData PollData();
    protected abstract void Update(in TData data);
}
