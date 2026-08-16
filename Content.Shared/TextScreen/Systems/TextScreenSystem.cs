using System.Text;
using Content.Shared.TextScreen.Components;
using Robust.Shared.Timing;

namespace Content.Shared.TextScreen.Systems;

/// <summary>
/// The TextScreenSystem draws text in the game world using 3x5 sprite states for each character.
/// It supports scrolling text, and the use of timers through <see cref="TextScreenTimerComponent"/>
/// The bulk of the
/// </summary>
/// <seealso cref="TextScreenComponent"/>
public abstract partial class TextScreenSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;

    /// <summary>
    /// The maximum number of characters to display per line when scrolled.
    /// </summary>
    private const int MaxScrollingCharacters = 32;

    private static readonly string[] LineBreaks = new[] { "\r\n", "\n" };

    private StringBuilder _builder = new();

    #region Public API
    /// <summary>
    /// Sets the string to be displayed for a given entity.
    /// </summary>
    /// <param name="ent">The timer to set the display.</param>
    /// <param name="text">The text to display on the screen.</param>
    /// <param name="time">An optional time to say the message arrived at. If null, uses the current time.</param>
    public void SetString(Entity<TextScreenComponent?> ent, string text, TimeSpan? time = null)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var strings = text.Split(LineBreaks, StringSplitOptions.None);
        _builder.Clear();
        for (int i = 0; i < ent.Comp.Rows; i++)
        {
            if (i < strings.Length)
                _builder.Append(strings[i].Substring(0, int.Min(strings[i].Length, MaxScrollingCharacters)));
            if (i != ent.Comp.Rows - 1)
                _builder.Append("\n");
        }

        ent.Comp.Text = _builder.ToString();
        ent.Comp.TextTime = time ?? _timing.CurTime;
        Dirty(ent);
    }

    /// <summary>
    /// Sets the color of the text displayed on the screen.
    /// </summary>
    /// <param name="ent">The screen to change.</param>
    /// <param name="color">The color to draw the text on the screen.</param>
    public void SetColor(Entity<TextScreenComponent?> ent, Color color)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.Color = color;
        Dirty(ent);
    }

    /// <summary>
    /// Sets the string to be displayed for a given entity.
    /// </summary>
    /// <param name="ent">The timer to set the display.</param>
    /// <param name="text">The text to .</param>
    /// <param name="ent">The timer to set the display.</param>
    public void SetTimerStrings(Entity<TextScreenTimerComponent?> ent, string finishedText, string runningText = "")
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.FinishedText = finishedText;
        ent.Comp.RunningText = runningText;
        Dirty(ent);

        if (ent.Comp.TargetTime is null)
            SetString(ent.Owner, ent.Comp.FinishedText);
    }

    /// <summary>
    /// Sets the string to be displayed for a given entity.
    /// </summary>
    /// <param name="ent">The timer to set the display.</param>
    /// <param name="time">The time the timer should count down to.  Set to null to clear the timer.</param>
    public void SetTimerTarget(Entity<TextScreenTimerComponent?> ent, TimeSpan? time)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.TargetTime = time;
        Dirty(ent);
    }

    /// <summary>
    /// Returns the <paramref name="timeSpan"/> converted to a string in either HH:MM, MM:SS or potentially SS:mm format.
    /// </summary>
    /// <param name="timeSpan">TimeSpan to convert into string.</param>
    /// <param name="getMilliseconds">Should the string be ss:ms if minutes are less than 1?</param>
    /// <remarks>
    /// hours, minutes, seconds, and centiseconds are each set to 2 decimal places by default.
    /// </remarks>
    public static string TimeToString(TimeSpan timeSpan, bool getMilliseconds = true, string hours = "D2", string minutes = "D2", string seconds = "D2", string cs = "D2")
    {
        string firstString;
        string lastString;

        if (timeSpan.TotalHours >= 1)
        {
            firstString = timeSpan.Hours.ToString(hours);
            lastString = timeSpan.Minutes.ToString(minutes);
        }
        else if (timeSpan.TotalMinutes >= 1 || !getMilliseconds)
        {
            firstString = timeSpan.Minutes.ToString(minutes);
            lastString = timeSpan.Seconds.ToString(seconds);
        }
        else
        {
            firstString = timeSpan.Seconds.ToString(seconds);
            var centiseconds = timeSpan.Milliseconds / 10;
            lastString = centiseconds.ToString(cs);
        }

        return firstString + ':' + lastString;
    }
    #endregion Public API

    #region Event Handlers
    /// <summary>
    /// Handles non-trivial pause timing for scrolling.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnUnpaused(Entity<TextScreenComponent> ent, ref EntityUnpausedEvent args)
    {
        for (int i = 0; i < ent.Comp.RowData.Length; i++)
            AddTime(ref ent.Comp.RowData[i], args.PausedTime);
    }

    /// <summary>
    /// Adds <paramref name="time"/> to the row's scrolling timer.
    /// </summary>
    private void AddTime(ref TextScreenRow row, TimeSpan time)
    {
        row.NextScroll += time;
    }
    #endregion Event Handlers
}
