using System.Linq;
using System.Numerics;
using Content.Shared.TextScreen;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.TextScreen;

// Overview:
// Data is passed from server to client through <see cref="SharedAppearanceSystem.SetData"/>,
// calling <see cref="OnAppearanceChange"/>, which calls almost everything else.

// Data for the (at most one) timer is stored in <see cref="TextScreenTimerComponent"/>.

// All screens have <see cref="TextScreenVisualsComponent"/>, but:
// the update method only updates the timers, so the timercomp is added/removed by appearance changes/timing out.

// Because the sprite component stores layers in a dict with no nesting, individual layers
// have to be mapped to unique ids e.g. {"textMapKey01" : <b>{first row, second char layerstate}</b>}
// in either the visuals or timer component.


/// <summary>
/// The TextScreenSystem draws text in the game world using 3x5 sprite states for each character.
/// </summary>
public sealed partial class TextScreenSystem : VisualizerSystem<TextScreenVisualsComponent>
{
    [Dependency] private IGameTiming _gameTiming = default!;

    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery = default!;
    [Dependency] private EntityQuery<TextScreenTimerComponent> _screenTimerQuery = default!;

    /// <summary>
    /// Contains char/state Key/Value pairs. <br/>
    /// The states in Textures/Effects/text.rsi that special character should be replaced with.
    /// </summary>
    private static readonly Dictionary<char, string> CharStatePairs = new()
        {
            { '<', "angle-l" },
            { '>', "angle-r" },
            {'\'', "apostrophe" },
            {'\\', "backslash" },
            { ' ', "blank" },
            { '[', "bracket-l" },
            { ']', "bracket-r" },
            { '^', "caret" },
            { ':', "colon" },
            { ',', "comma" },
            { '-', "dash" },
            { '=', "equals" },
            { '!', "exclamation" },
            { '#', "hash" },
            { '(', "paren-l" },
            { ')', "paren-r" },
            { '%', "percent" },
            { '.', "period" },
            { '+', "plus" },
            { '?', "question" },
            { '"', "quotation" },
            { ';', "semicolon" },
            { '/', "slash" },
            { '$', "speso" },
            { '*', "star" },
            { '_', "underscore" },
        };

    private const string DefaultState = "blank";

    /// <summary>
    /// A string prefix for all text layers.
    /// </summary>
    private const string TextMapKey = "textMapKey";

    /// <summary>
    /// The path to the RSI containing the text sprites.
    /// </summary>
    private const string TextPath = "Effects/text.rsi";

    /// <summary>
    /// The width of an individual character, in pixels.
    /// </summary>
    private const int CharWidth = 4;

    /// <summary>
    /// The maximum number of characters to display per line when scrolled.
    /// </summary>
    private const int MaxScrollingCharacters = 32;

    /// <summary>
    /// The longest that a message should take to cross the screen before wrapping around.
    /// </summary>
    private static readonly TimeSpan MaxMessageScrollTime = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The longest that it should take to scroll one pixel on a screen.
    /// </summary>
    private static readonly TimeSpan MaxPixelScrollTime = TimeSpan.FromMilliseconds(100);

    #region Inherited
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        UpdatesOutsidePrediction = true;
    }

    /// <summary>
    /// Called by <see cref="SharedAppearanceSystem.SetData"/> to handle text updates,
    /// and spawn a <see cref="TextScreenTimerComponent"/> if necessary
    /// </summary>
    /// <remarks>
    /// The appearance updates are batched; order matters for both sender and receiver.
    /// </remarks>
    protected override void OnAppearanceChange(EntityUid uid, TextScreenVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (!Resolve(uid, ref args.Sprite))
            return;

        if (args.AppearanceData.TryGetValue(TextScreenVisuals.Color, out var color) && color is Color)
            component.Color = (Color)color;

        // DefaultText: fallback text e.g. broadcast updates from comms consoles
        if (args.AppearanceData.TryGetValue(TextScreenVisuals.DefaultText, out var newDefault) && newDefault is string)
            component.Text = SegmentText((string)newDefault, component);

        // ScreenText: currently rendered text e.g. the "ETA" accompanying shuttle timers
        if (args.AppearanceData.TryGetValue(TextScreenVisuals.ScreenText, out var screenText) && screenText is string text && text != component.LastText)
        {
            TimeSpan? startTime = null;
            if (args.AppearanceData.TryGetValue(TextScreenVisuals.ScreenTextTime, out var screenTextTime) && screenTextTime is TimeSpan scrollStart)
                startTime = scrollStart;

            component.TextToDraw = SegmentText(text, component);
            ResetText((uid, component));
            BuildTextLayers((uid, component, args.Sprite));
            DrawLayers(uid, component.LayerStatesToDraw);
            ResetScrollingState((uid, component), startTime);
        }

        if (!args.AppearanceData.TryGetValue(TextScreenVisuals.TargetTime, out var time)
            || time is not TimeSpan target)
            return;

        if (target > _gameTiming.CurTime)
        {
            var timer = EnsureComp<TextScreenTimerComponent>(uid);
            timer.Target = target;
            BuildTimerLayers((uid, timer, component));
            DrawLayers(uid, timer.LayerStatesToDraw);
        }
        else
        {
            TeardownTimer((uid, component));
        }
    }

    /// <summary>
    /// Update handler - keep timers and scrolling text up to date.
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TextScreenVisualsComponent>();
        while (query.MoveNext(out var uid, out var screen))
        {
            if (_screenTimerQuery.TryComp(uid, out var timer))
            {
                if (_gameTiming.CurTime < timer.Target)
                {
                    BuildTimerLayers((uid, timer, screen));
                    DrawLayers(uid, timer.LayerStatesToDraw);
                }
                else
                {
                    TeardownTimer((uid, screen));
                }
            }
            else if (screen.ScrollEnabled && screen.NextScrollTime.Any(x => x < _gameTiming.CurTime))
            {
                DrawScrolledLayers((uid, screen));
            }
        }
    }
    #endregion Inherited

    #region Public API
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

    /// <summary>
    /// Returns the Effects/text.rsi state string based on <paramref name="character"/>, or null if none available.
    /// </summary>
    public static string? GetStateFromChar(char? character)
    {
        if (character == null)
            return null;

        // First checks if its one of our special characters
        if (CharStatePairs.TryGetValue(character.Value, out var value))
            return value;

        // Or else it checks if its a normal letter or digit
        if (char.IsLetterOrDigit(character.Value))
            return character.Value.ToString().ToLower();

        return null;
    }
    #endregion Public API

    #region Event Handlers
    [SubscribeLocalEvent]
    private void OnInit(Entity<TextScreenVisualsComponent> ent, ref ComponentInit args)
    {
        if (!_spriteQuery.TryComp(ent, out var sprite))
            return;

        // awkward to specify a textoffset of e.g. 0.1875 in the prototype
        ent.Comp.TextOffset = Vector2.Multiply(TextScreenVisualsComponent.PixelSize, ent.Comp.TextOffset);
        ent.Comp.TimerOffset = Vector2.Multiply(TextScreenVisualsComponent.PixelSize, ent.Comp.TimerOffset);

        ResetText((ent, ent.Comp, sprite));
        BuildTextLayers((ent, ent.Comp, sprite));
    }

    /// <summary>
    /// Handles non-trivial pause timing for scrolling.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnUnpaused(Entity<TextScreenVisualsComponent> ent, ref EntityUnpausedEvent args)
    {
        for (int i = 0; i < ent.Comp.NextScrollTime.Length; i++)
        {
            if (ent.Comp.NextScrollTime[i] != TimeSpan.MaxValue) // Reserved value, should stay at max.
                ent.Comp.NextScrollTime[i] += args.PausedTime;
        }
    }
    #endregion Event Handlers

    #region Internal
    /// <summary>
    /// Removes the timer component, clears the sprite layer dict,
    /// and draws <see cref="TextScreenVisualsComponent.Text"/>
    /// </summary>
    private void TeardownTimer(Entity<TextScreenVisualsComponent> ent)
    {
        ent.Comp.TextToDraw = ent.Comp.Text;

        if (!_screenTimerQuery.TryComp(ent, out var timer) || !_spriteQuery.TryComp(ent, out var sprite))
            return;

        foreach (var key in timer.LayerStatesToDraw.Keys)
            SpriteSystem.RemoveLayer((ent, sprite), key);

        RemComp<TextScreenTimerComponent>(ent);

        ResetText(ent);
        BuildTextLayers((ent.Owner, ent.Comp, sprite));
        DrawLayers(ent.Owner, ent.Comp.LayerStatesToDraw);
    }

    /// <summary>
    /// Converts string to string?[] based on
    /// <see cref="TextScreenVisualsComponent.RowLength"/> and <see cref="TextScreenVisualsComponent.Rows"/>.
    /// </summary>
    private string?[] SegmentText(string text, TextScreenVisualsComponent component)
    {
        var segmented = new string?[component.Rows];

        // Split by newlines, reduce each line to MaxCharacters
        var sublines = text.Split("\n");
        var length = int.Min(component.Rows, sublines.Length);
        for (var i = 0; i < length; i++)
        {
            var line = sublines[i].Trim();

            // Ensure our string's length is within our limits.
            var maxLength = component.ScrollEnabled ? MaxScrollingCharacters : component.RowLength;
            if (line.Length > maxLength)
                line = line.Substring(0, MaxScrollingCharacters);

            // If the text will scroll, ensure that we have a buffer between lines.
            if (line.Length > component.RowLength)
                line = line.PadRight(line.Length + component.RowLength - 1);

            segmented[i] = line;
        }

        return segmented;
    }

    /// <summary>
    /// Clears <see cref="TextScreenVisualsComponent.LayerStatesToDraw"/>, and instantiates new blank defaults.
    /// </summary>
    private void ResetText(Entity<TextScreenVisualsComponent, SpriteComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp2))
            return;

        var screen = ent.Comp1;
        var sprite = (ent.Owner, ent.Comp2);

        foreach (var key in screen.LayerStatesToDraw.Keys)
            SpriteSystem.RemoveLayer(sprite, key, logMissing: false);

        screen.LayerStatesToDraw.Clear();

        for (var row = 0; row < screen.Rows; row++)
        {
            for (var i = 0; i < screen.RowLength + 1; i++) // Extra index needed for scrolling.
            {
                var key = TextMapKey + row + i;
                var layerIndex = SpriteSystem.LayerMapReserve(sprite, key);
                screen.LayerStatesToDraw.Add(key, null);
                SpriteSystem.LayerSetRsi(sprite, layerIndex, new ResPath(TextPath));
                SpriteSystem.LayerSetColor(sprite, layerIndex, screen.Color);
                SpriteSystem.LayerSetRsiState(sprite, layerIndex, DefaultState);
            }
        }

        if (screen.FrameState != null)
        {
            var key = TextScreenVisualLayers.Frame;
            SpriteSystem.RemoveLayer(sprite, key, logMissing: false); // State may not exist, remove it if it does - needs to be on top of the text.
            var layerIndex = SpriteSystem.LayerMapReserve(sprite, key);
            SpriteSystem.LayerSetData(sprite, layerIndex, screen.FrameState);
        }
    }

    /// <summary>
    /// Sets the states in the <see cref="TextScreenVisualsComponent.LayerStatesToDraw"/> to match the component
    /// <see cref="TextScreenVisualsComponent.TextToDraw"/> string?[].
    /// </summary>
    /// <remarks>
    /// Remember to set <see cref="TextScreenVisualsComponent.TextToDraw"/> to a string?[] first.
    /// </remarks>
    private void BuildTextLayers(Entity<TextScreenVisualsComponent, SpriteComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp2))
            return;

        var screen = ent.Comp1;
        var sprite = (ent.Owner, ent.Comp2);

        for (var rowIdx = 0; rowIdx < Math.Min(screen.TextToDraw.Length, screen.Rows); rowIdx++)
        {
            var row = screen.TextToDraw[rowIdx];
            if (row == null)
                continue;

            var min = Math.Min(row.Length, screen.RowLength);

            for (var chr = 0; chr < min; chr++)
            {
                screen.LayerStatesToDraw[TextMapKey + rowIdx + chr] = GetStateFromChar(row[chr]);
                SpriteSystem.LayerSetOffset(
                    sprite,
                    TextMapKey + rowIdx + chr,
                    screen.TextOffset + Vector2.Multiply(
                        new Vector2((chr - min / 2f + 0.5f) * CharWidth, -rowIdx * screen.RowOffset),
                        TextScreenVisualsComponent.PixelSize)
                );
            }
        }
    }

    /// <summary>
    /// Populates timer.LayerStatesToDraw and the sprite component's layer dict with calculated offsets.
    /// </summary>
    private void BuildTimerLayers(Entity<TextScreenTimerComponent, TextScreenVisualsComponent, SpriteComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp3))
            return;

        var timer = ent.Comp1;
        var screen = ent.Comp2;
        var sprite = (ent.Owner, ent.Comp3);

        var time = TimeToString(
            (_gameTiming.CurTime - timer.Target).Duration(),
            false,
            screen.HourFormat, screen.MinuteFormat, screen.SecondFormat
        );

        var min = Math.Min(time.Length, screen.RowLength);

        for (var i = 0; i < min; i++)
        {
            var layer = TextMapKey + 0 + i;
            timer.LayerStatesToDraw[layer] = GetStateFromChar(time[i]);
            SpriteSystem.LayerSetOffset(
                sprite,
                layer,
                screen.TimerOffset + Vector2.Multiply(
                    new Vector2((i - min / 2f + 0.5f) * CharWidth, 0f),
                    TextScreenVisualsComponent.PixelSize)
            );
        }
    }

    /// <summary>
    /// Draws a LayerStates dict by setting the sprite states individually.
    /// </summary>
    private void DrawLayers(Entity<SpriteComponent?> ent, Dictionary<string, string?> layerStates)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        foreach (var (key, state) in layerStates.Where(pairs => pairs.Value != null))
            SpriteSystem.LayerSetRsiState(ent, key, state);
    }

    /// <summary>
    /// Handles scrolling, updates the scrolled state of a text screen.
    /// </summary>
    /// <remarks>
    /// Be sure to call BuildTimerLayers before using this to set up the text layers used.
    /// </remarks>
    private void DrawScrolledLayers(Entity<TextScreenVisualsComponent, SpriteComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp2))
            return;

        var screen = ent.Comp1;
        var sprite = (ent.Owner, ent.Comp2);

        for (int i = 0; i < screen.Rows; i++)
        {
            bool scrolled = false;
            bool newChar = false;
            // Find the number of pixels we need to scroll.
            if (screen.NextScrollTime[i] < _gameTiming.CurTime && screen.TimeBetweenScrolls[i] > TimeSpan.Zero)
            {
                var difference = (_gameTiming.CurTime - screen.NextScrollTime[i]).TotalSeconds;
                var increments = (int)Math.Truncate(difference / screen.TimeBetweenScrolls[i].TotalSeconds) + 1;

                newChar = increments >= CharWidth || CharacterWrapped(screen.ScrollPosition[i], increments);
                scrolled = true;

                screen.ScrollPosition[i] += increments;
                screen.NextScrollTime[i] += increments * screen.TimeBetweenScrolls[i];
            }

            if (!scrolled)
                continue;

            var charOffset = screen.ScrollPosition[i] % CharWidth; // The amount to scroll each character off to the left by.
            for (int j = 0; j < screen.RowLength + 1; j++)
            {
                SpriteSystem.LayerSetOffset(
                    sprite,
                    TextMapKey + i + j,
                    Vector2.Multiply(
                        new Vector2((j - screen.RowLength / 2f + 0.5f) * CharWidth - charOffset, -i * screen.RowOffset),
                        TextScreenVisualsComponent.PixelSize
                        ) + screen.TextOffset
                );
            }

            if (!newChar)
                continue;

            var textOffset = screen.ScrollPosition[i] / CharWidth; // The total number of characters scrolled so far.
            for (int j = 0; j < screen.RowLength + 1; j++)
            {
                var chr = (textOffset + j) % screen.TextToDraw[i]!.Length;
                SpriteSystem.LayerSetRsiState(
                    sprite,
                    TextMapKey + i + j,
                    GetStateFromChar(screen.TextToDraw[i]![chr])
                );
            }
        }
    }

    /// <summary>
    /// Returns true if <paramref name=oldValue"/> wraps onto a
    /// new character if incremented by <paramref name="increments"/>
    /// </summary>
    private bool CharacterWrapped(int oldValue, int increments)
    {
        var newValue = oldValue + increments;
        return newValue % CharWidth < oldValue % CharWidth;
    }

    /// <summary>
    /// Resets the scrolling state for a particular text screen.
    /// </summary>
    private void ResetScrollingState(Entity<TextScreenVisualsComponent> ent, TimeSpan? startTime)
    {
        if (!ent.Comp.ScrollEnabled)
            return;

        for (int i = 0; i < ent.Comp.Rows; i++)
        {
            // Short/null string, shouldn't scroll.
            if (ent.Comp.TextToDraw[i] == null || ent.Comp.TextToDraw[i]!.Length <= ent.Comp.RowLength)
            {
                ent.Comp.NextScrollTime[i] = TimeSpan.MaxValue;
                ent.Comp.TimeBetweenScrolls[i] = TimeSpan.MaxValue;
            }
            else
            {
                // Find our desired scroll speed.
                var newMaxScrollTime = MaxMessageScrollTime / ent.Comp.TextToDraw[i]!.Length / CharWidth;
                var scrollTime = newMaxScrollTime < MaxPixelScrollTime ? newMaxScrollTime : MaxPixelScrollTime;
                ent.Comp.NextScrollTime[i] = (startTime ?? _gameTiming.CurTime) + scrollTime;
                ent.Comp.TimeBetweenScrolls[i] = scrollTime;
            }
            ent.Comp.ScrollPosition[i] = 0;
        }
    }
    #endregion Internal
}
