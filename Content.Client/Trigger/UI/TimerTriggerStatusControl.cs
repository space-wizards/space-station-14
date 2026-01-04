using Content.Client.Items.UI;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Trigger.Components;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Trigger.UI;

/// <summary>
/// Displays timer delay information for <see cref="TimerTriggerComponent"/> from predicted client state.
/// </summary>
/// <seealso cref="TimerTriggerItemStatusSystem"/>
public sealed class TimerTriggerStatusControl : PollingItemStatusControl<TimerTriggerStatusControl.Data>
{
    private readonly Entity<TimerTriggerComponent> _parent;
    private readonly RichTextLabel _label;

    public TimerTriggerStatusControl(
        Entity<TimerTriggerComponent> parent)
    {
        _parent = parent;
        _label = new RichTextLabel { StyleClasses = { StyleClass.ItemStatus } };
        AddChild(_label);
    }

    protected override Data PollData()
    {
        return new Data(_parent.Comp.Delay);
    }

    protected override void Update(in Data data)
    {
        var markup = Loc.GetString("timer-trigger-status-delay", ("delay", data.Delay.TotalSeconds));
        _label.SetMarkup(markup);
    }

    public readonly record struct Data(TimeSpan Delay);
}
