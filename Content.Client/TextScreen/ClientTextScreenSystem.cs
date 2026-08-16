using System.Text;
using Content.Shared.TextScreen.Components;

namespace Content.Shared.TextScreen.Systems;

/// <inheritdoc/>
public sealed partial class ClientTextScreenSystem : TextScreenSystem
{
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
    public void SetString(Entity<TextScreenComponent?> ent, string input)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var strings = input.Split(LineBreaks, StringSplitOptions.None);
        _builder.Clear();
        for (int i = 0; i < ent.Comp.Rows; i++)
        {
            if (i < strings.Length)
                _builder.Append(strings[i].Substring(0, int.Min(strings[i].Length, MaxScrollingCharacters)));
            if (i != ent.Comp.Rows - 1)
                _builder.Append("\n");
        }

        ent.Comp.Text = _builder.ToString();
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
