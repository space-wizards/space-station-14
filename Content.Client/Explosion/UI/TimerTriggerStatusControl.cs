using Content.Client.Items.UI;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Explosion;
using Robust.Client.UserInterface.Controls;
using System;

namespace Content.Client.Explosion.UI;

/// <summary>
/// Displays timer delay information for <see cref="TimerTriggerItemStatusComponent"/>.
/// </summary>
/// <seealso cref="TimerTriggerItemStatusSyncSystem"/>
public sealed class TimerTriggerStatusControl : PollingItemStatusControl<TimerTriggerStatusControl.Data>
{
    private readonly Entity<TimerTriggerItemStatusComponent> _parent;
    private readonly RichTextLabel _label;

    public TimerTriggerStatusControl(
        Entity<TimerTriggerItemStatusComponent> parent)
    {
        _parent = parent;
        _label = new RichTextLabel { StyleClasses = { StyleNano.StyleClassItemStatus } };
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
