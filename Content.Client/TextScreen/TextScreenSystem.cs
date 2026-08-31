using System.Numerics;
using Content.Client.TextScreen.Components;
using Content.Shared.TextScreen.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.TextScreen.Systems;

/// <summary>
/// The TextScreenSystem draws text in the game world using 3x5 sprite states for each character.
/// It optionally supports scrolling text.
/// </summary>
/// <remarks>
/// Not using a VisualizerSystem since this cares about two different components.
/// </remarks>
/// <seealso cref="TextScreenComponent"/>
/// <seealso cref="TextScreenTimerComponent"/>
public sealed partial class TextScreenSystem : VisualizerSystem<TextScreenComponent>
{
    [Dependency] private IGameTiming _timing = default!;

    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery;
    [Dependency] private EntityQuery<TextScreenTimerComponent> _screenTimerQuery;

    /// <summary>
    /// Contains char/state Key/Value pairs. <br/>
    /// The states in Textures/Effects/text.rsi that special character should be replaced with.
    /// </summary>
    private static readonly Dictionary<char, string> CharStatePairs = new() {
        { '<', "angle-l" },
        { '>', "angle-r" },
        {'\'', "apostrophe" },
        {'\\', "backslash" },
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
    /// The maximum number of characters to display per row.
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

    #region Public API
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        UpdatesOutsidePrediction = true;
    }

    /// <summary>
    /// Update handler - keep timers and scrolling text up to date.
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var timerQuery = EntityQueryEnumerator<TextScreenTimerComponent>();
        while (timerQuery.MoveNext(out var uid, out var timer))
        {
            if (timer.TargetTime == null)
                continue;

            if (timer.TargetTime <= _timing.CurTime)
            {
                SetTextToDisplay(uid, timer.FinishedText);
                UpdateTimerSprite((uid, timer), false);
                timer.TargetTime = null;
                timer.ScreenValue = 0;
            }
            else
            {
                int screenValue = ConvertTimeToScreenValue(timer.TargetTime.Value, _timing.CurTime, timer.ShowCentiseconds);
                if (screenValue != timer.ScreenValue)
                {
                    var timerText = GetTimerString((uid, timer), screenValue);
                    SetTextToDisplay(uid, timerText);
                    UpdateTimerSprite((uid, timer), true);
                    timer.ScreenValue = screenValue;
                }
            }
        }

        var screenQuery = EntityQueryEnumerator<TextScreenComponent, SpriteComponent>();
        while (screenQuery.MoveNext(out var uid, out var screen, out var sprite))
        {
            if (screen.NewTextToDisplay)
            {
                // Update text
                DrawNewText((uid, screen));
                screen.NewTextToDisplay = false;
            }

            // Handle scrolling.
            if (screen.ScrollEnabled)
            {
                for (int i = 0; i < screen.RowData.Length; i++)
                {
                    var rowData = screen.RowData[i];
                    if (rowData.NextScroll <= _timing.CurTime)
                    {
                        ScrollRow(ref rowData);
                        DrawLayers((uid, screen, sprite), rowData, i);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Converts the difference between two timespans into a value between 0 and 9999.
    /// </summary>
    public int ConvertTimeToScreenValue(TimeSpan targetTime, TimeSpan curTime, bool showCentiseconds)
    {
        if (targetTime <= curTime)
            return 0;

        var difference = targetTime - curTime;
        var millis = difference.TotalMilliseconds;
        if (showCentiseconds && millis < 100_000) // 9999 centiseconds, 99:99, the largest value that could fit in two fields.
            return (int)millis / 10;
        else if (millis < TimeSpan.MillisecondsPerHour)
            return difference.Minutes * 100 + difference.Seconds;
        else
            return difference.Hours * 100 + difference.Minutes;
    }

    /// <summary>
    /// Converts the difference between two timespans into a value between 0 and 9999.
    /// </summary>
    public void SetTextToDisplay(Entity<TextScreenComponent?> ent, string? text)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.TextToDisplay == text)
            return;

        ent.Comp.TextToDisplay = text;
        ent.Comp.NewTextToDisplay = true;
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
    /// Handles updates from the server.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnStartup(Entity<TextScreenComponent> ent, ref ComponentStartup args)
    {
        if (!_spriteQuery.TryComp(ent, out var sprite))
            return;

        if (ent.Comp.CurrentColor == default)
            ent.Comp.CurrentColor = ent.Comp.Color;

        // Create text layers
        var textRsiPath = new ResPath(TextPath);
        for (var rowIdx = 0; rowIdx < ent.Comp.RowData.Length; rowIdx++)
        {
            var maxIndex = ent.Comp.ScrollEnabled ? ent.Comp.RowLength + 1 : ent.Comp.RowLength;
            var textScreenRow = ent.Comp.RowData[rowIdx];

            for (var chr = 0; chr < maxIndex; chr++)
            {
                var newKey = TextMapKey + rowIdx + chr;
                var layerIndex = SpriteSystem.LayerMapReserve((ent, sprite), newKey);
                SpriteSystem.LayerSetRsi((ent, sprite), layerIndex, textRsiPath, null);
                SpriteSystem.LayerSetColor((ent, sprite), layerIndex, ent.Comp.CurrentColor);
                textScreenRow.Layers.Add((newKey, null));
            }
        }

        // Place frame on top of text layers (obscuring the scroll trick)
        if (ent.Comp.FrameState != null)
            SpriteSystem.AddLayer((ent, sprite), ent.Comp.FrameState, null);
    }

    /// <summary>
    /// Handles updates from the server.
    /// </summary>
    protected override void OnAppearanceChange(EntityUid uid, TextScreenComponent comp, ref AppearanceChangeEvent args)
    {
        bool anyChange;
        if (args.TryGetData(TextScreenVisuals.Color, out Color color))
        {
            anyChange = comp.CurrentColor != color;
            comp.CurrentColor = color;
        }
        else
        {
            anyChange = comp.CurrentColor != comp.Color;
            comp.CurrentColor = comp.Color;
        }

        // Update layer color - less frequent to change, no need to change in update.
        if (anyChange && _spriteQuery.TryComp(uid, out var sprite))
        {
            foreach (var row in comp.RowData)
            {
                foreach (var layer in row.Layers)
                    SpriteSystem.LayerSetColor((uid, sprite), layer.Key, comp.CurrentColor);
            }
        }

        //
        args.TryGetData(TextScreenVisuals.ScreenText, out string? screenTextValue);
        args.TryGetData(TextScreenVisuals.DefaultText, out string? defaultTextValue);

        if (!args.TryGetData(TextScreenVisuals.ScreenTextTime, out TimeSpan? scrollTime))
            scrollTime = _timing.CurTime;

        if (_screenTimerQuery.TryComp(uid, out var timer)
            && args.TryGetData(TextScreenVisuals.TargetTime, out TimeSpan? textTime))
        {
            // If we have a valid timer, draw the timer.
            if (defaultTextValue is { } defaultText && defaultText != timer.FinishedText)
            {
                timer.FinishedText = defaultText;
                // TODO: assign anyChange
            }
            if (screenTextValue is { } screenText && screenText != timer.RunningText)
            {
                timer.RunningText = screenText;
                anyChange = true;
            }
            if (textTime != timer.TargetTime)
            {
                timer.TargetTime = textTime;
                anyChange = true;
            }
            comp.TextTime = scrollTime.Value;
            comp.NewTextToDisplay = anyChange;
        }
        else
        {
            // Otherwise, if we have text, draw our text.
            var newTextValue = screenTextValue ?? defaultTextValue;
            if (newTextValue != comp.TextToDisplay)
            {
                comp.TextToDisplay = newTextValue;
                anyChange = true;
            }
            comp.TextTime = scrollTime.Value;
            comp.NewTextToDisplay = anyChange;
        }
    }

    private string GetTimerString(Entity<TextScreenTimerComponent> ent, int newScreenValue)
    {
        if (ent.Comp.TimerRow < 0)
            return ent.Comp.RunningText;

        var strings = ent.Comp.RunningText.Split("\n");
        var timerString = $"{newScreenValue / 100:D2}:{newScreenValue % 100:D2}";
        if (ent.Comp.TimerRow < strings.Length)
        {
            strings[ent.Comp.TimerRow] = timerString;
        }
        else
        {
            // Extend our array until we have a timer row.
            var newStrings = new string[ent.Comp.TimerRow + 1];
            for (int i = 0; i < strings.Length; i++)
                newStrings[i] = strings[i];
            for (int i = strings.Length; i < ent.Comp.TimerRow; i++)
                newStrings[i] = "";
            newStrings[ent.Comp.TimerRow] = timerString;

            strings = newStrings;
        }
        return string.Join('\n', strings);
    }

    private void DrawNewText(Entity<TextScreenComponent> ent)
    {
        // No sprite, put in a default state.
        if (!_spriteQuery.TryComp(ent, out var sprite))
        {
            for (var i = 0; i < ent.Comp.RowData.Length; i++)
            {
                var rowData = ent.Comp.RowData[i];
                rowData.NextScroll = TimeSpan.MaxValue;
                rowData.ScrollDelay = TimeSpan.MaxValue;
                rowData.ScrollPosition = 0;
                rowData.Text = "";

                ent.Comp.RowData[i] = rowData;
            }
            return;
        }

        var texts = ent.Comp.TextToDisplay?.Split("\n") ?? [];

        for (var i = 0; i < ent.Comp.RowData.Length; i++)
        {
            var rowData = ent.Comp.RowData[i];

            if (i >= texts.Length || texts[i].Length == 0)
            {
                for (var j = 0; j < rowData.Layers.Count; j++)
                {
                    var layerTuple = rowData.Layers[j];

                    if (SpriteSystem.LayerMapTryGet((ent, sprite), layerTuple.Key, out var layerIndex, false))
                        SpriteSystem.LayerSetRsiState((ent, sprite), layerIndex, null);

                    rowData.Layers[j] = new(layerTuple.Key, null);
                }
                rowData.ScrollDelay = TimeSpan.MaxValue;
                rowData.NextScroll = TimeSpan.MaxValue;
                rowData.ScrollPosition = 0;
                rowData.Text = "";
            }
            else
            {
                if (texts[i].Length <= ent.Comp.RowLength)
                {
                    rowData.ScrollDelay = TimeSpan.MaxValue;
                    rowData.NextScroll = TimeSpan.MaxValue;
                    rowData.ScrollPosition = 0;
                    rowData.Text = texts[i];
                }
                else
                {
                    var newMaxScrollTime = MaxMessageScrollTime / texts[i].Length / CharWidth;
                    rowData.ScrollDelay = newMaxScrollTime < MaxPixelScrollTime ? newMaxScrollTime : MaxPixelScrollTime;
                    rowData.NextScroll = ent.Comp.TextTime;
                    rowData.ScrollPosition = 0;
                    var rowText = texts[i].Substring(0, int.Min(texts[i].Length, MaxScrollingCharacters));
                    rowData.Text = rowText.PadRight(rowText.Length + ent.Comp.RowLength - 1);
                    ScrollRow(ref rowData);
                }

                DrawLayers((ent.Owner, ent.Comp, sprite), rowData, i);
            }

            ent.Comp.RowData[i] = rowData;
        }
    }

    private void ScrollRow(ref TextScreenRow rowData)
    {
        var difference = (_timing.CurTime - rowData.NextScroll).TotalSeconds;
        var increments = (int)Math.Truncate(difference / rowData.ScrollDelay.TotalSeconds) + 1;
        rowData.ScrollPosition += increments;
        rowData.NextScroll += increments * rowData.ScrollDelay;
    }

    private void DrawLayers(Entity<TextScreenComponent, SpriteComponent> ent, TextScreenRow rowData, int rowIndex)
    {
        Entity<SpriteComponent?> sprite = (ent.Owner, ent.Comp2);
        var screen = ent.Comp1;

        var textIsScrolling = rowData.Text.Length > screen.RowLength;
        var scrollOffset = textIsScrolling ? screen.HorizontalScrollOffset : 0;

        var maxCharIndex = int.Min(rowData.Layers.Count, rowData.Text.Length);
        var subCharOffset = rowData.ScrollPosition % CharWidth;
        for (var j = 0; j < maxCharIndex; j++)
        {
            var layerTuple = rowData.Layers[j];
            var charIndex = (j + rowData.ScrollPosition / CharWidth) % rowData.Text.Length;

            var newState = GetStateFromChar(rowData.Text[charIndex]);

            if (SpriteSystem.LayerMapTryGet(sprite, layerTuple.Key, out var layerIndex, false))
            {
                SpriteSystem.LayerSetRsiState(sprite, layerIndex, newState);
                SpriteSystem.LayerSetOffset(sprite, layerIndex, Vector2.Multiply(
                    screen.TextOffset +
                    new Vector2((j - maxCharIndex / 2f + 0.5f) * CharWidth - subCharOffset + scrollOffset, -rowIndex * screen.RowOffset),
                    TextScreenComponent.PixelSize));
            }

            rowData.Layers[j] = new(layerTuple.Key, newState);
        }
        for (var j = maxCharIndex; j < rowData.Layers.Count; j++)
        {
            var layerTuple = rowData.Layers[j];

            if (SpriteSystem.LayerMapTryGet((ent, sprite), layerTuple.Key, out var layerIndex, false))
                SpriteSystem.LayerSetRsiState((ent, sprite), layerIndex, null);

            rowData.Layers[j] = new(layerTuple.Key, null);
        }

        if (rowData.Layers.Count > 0)
        {
            var hideLeft = textIsScrolling && subCharOffset >= CharWidth - screen.LeftInvisiblePixels;
            var hideRight = textIsScrolling && subCharOffset < screen.RightInvisiblePixels;
            SpriteSystem.LayerSetVisible((ent, sprite), rowData.Layers[0].Key, !hideLeft);
            SpriteSystem.LayerSetVisible((ent, sprite), rowData.Layers[^1].Key, !hideRight);
        }
    }

    private void UpdateTimerSprite(Entity<TextScreenTimerComponent> ent, bool running)
    {
        if (_spriteQuery.TryComp(ent, out var sprite)
            && SpriteSystem.LayerMapTryGet((ent, sprite), TimerVisualLayers.Light, out var layerIndex, logMissing: false))
        {
            SpriteSystem.LayerSetRsiState((ent, sprite), layerIndex, running ? ent.Comp.RunningState : ent.Comp.FinishedState);
        }
    }
    #endregion Event Handlers
}
