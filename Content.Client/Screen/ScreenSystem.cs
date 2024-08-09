using System.Linq;
using System.Numerics;
using Content.Shared.Screen;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Screen;

/// overview:
/// Data is passed as a <see cref="ScreenUpdate"/> from server to client through <see cref="SharedAppearanceSystem.SetData"/>,
/// calling <see cref="OnAppearanceChange"/>, which calls almost everything else.

/// Data for the (at most one) timer is stored in <see cref="ScreenTimerComponent"/>.

/// All screens have <see cref="ScreenComponent"/>, but:
/// the update method only updates the timers, so the timercomp is added/removed by appearance changes/timing out.

/// Because the sprite component stores layers in a dict with no nesting, individual layers
/// have to be mapped to unique ids e.g. {"textMapKey01" : <first row, second char layerstate>}
/// in either the visuals or timer component.


/// <summary>
///     The ScreenSystem draws text in the game world using 3x5 sprite states for each character.
/// </summary>
public sealed class ScreenSystem : VisualizerSystem<ScreenComponent>
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    /// <summary>
    ///     Contains char/state Key/Value pairs. <br/>
    ///     The states in Textures/Effects/text.rsi that special character should be replaced with.
    /// </summary>
    private static readonly Dictionary<char, string> CharStatePairs = new()
        {
            { ':', "colon" },
            { '!', "exclamation" },
            { '?', "question" },
            { '*', "star" },
            { '+', "plus" },
            { '-', "dash" },
            { ' ', "blank" }
        };

    private const string DefaultState = "blank";

    /// <summary>
    ///     A string prefix for all text layers.
    /// </summary>
    private const string TextMapKey = "textMapKey";
    /// <summary>
    ///     A string prefix for all timer layers.
    /// </summary>
    private const string TimerMapKey = "timerMapKey";
    private const string TextPath = "Effects/text.rsi";
    private const int CharWidth = 4;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScreenComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ScreenTimerComponent, ComponentInit>(OnTimerInit);

        UpdatesOutsidePrediction = true;
    }

    private void OnInit(EntityUid uid, ScreenComponent component, ComponentInit args)
    {
        if (!TryComp(uid, out SpriteComponent? sprite))
            return;

        // awkward to specify a textoffset of e.g. 0.1875 in the prototype
        component.TextOffset = Vector2.Multiply(ScreenComponent.PixelSize, component.TextOffset);
        component.TimerOffset = Vector2.Multiply(ScreenComponent.PixelSize, component.TimerOffset);

        ClearLayerStates(uid, component, sprite);
    }

    /// <summary>
    ///     Instantiates <see cref="SpriteComponent.Layers"/> with {<see cref="TimerMapKey"/> + int : <see cref="DefaultState"/>} pairs.
    /// </summary>
    private void OnTimerInit(EntityUid uid, ScreenTimerComponent timer, ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) || !TryComp<ScreenComponent>(uid, out var screen))
            return;

        for (var i = 0; i < screen.RowLength; i++)
        {
            sprite.LayerMapReserveBlank(TimerMapKey + i);
            timer.LayerStatesToDraw.Add(TimerMapKey + i, null);
            sprite.LayerSetRSI(TimerMapKey + i, new ResPath(TextPath));
            sprite.LayerSetState(TimerMapKey + i, DefaultState);
        }
    }

    /// <summary>
    ///     Called by <see cref="SharedAppearanceSystem.SetData"/> to handle text updates,
    ///     and spawn a <see cref="ScreenTimerComponent"/> if necessary
    /// </summary>
    /// <remarks>
    ///     The appearance updates are batched; order matters for both sender and receiver.
    /// </remarks>
    protected override void OnAppearanceChange(EntityUid uid, ScreenComponent component, ref AppearanceChangeEvent args)
    {
        if (!Resolve(uid, ref args.Sprite) || !args.AppearanceData.TryGetValue(ScreenVisuals.Update, out var x) || x is not ScreenUpdate update)
            return;

        component.Updates[update.Priority] = update;

        RefreshActiveUpdate(uid, component);
    }

    /// this method is a hack because i'm still passing updates through the appearancesystem.
    /// deleting timers by setting them to TimeSpan.Zero in e.g. the emergencyshuttlesystem,
    /// instead of building a server -> client system for the screens makes timer handling awkwardly precise, because:
    /// ScreenUpdate deletion is lazy; recalling the shuttle leaves the zeroed timer in Updates until draw time.
    /// the next refactor if it ever happens should address the abuse of the appearancesystem
    /// <summary>
    ///     Draws the first member of <see cref="ScreenComponent.Updates"/>
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    private void RefreshActiveUpdate(EntityUid uid, ScreenComponent screen)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        ClearLayerStates(uid, screen, sprite);

        // nothing to draw
        if (screen.Updates.Count == 0)
            return;

        ScreenUpdate first = screen.Updates.First().Value;
        screen.ActiveUpdate = first;

        // color gets delegated to DrawLayers
        var text = first.Text;
        var target = first.Timer;
        var priority = first.Priority;

        if (target != null)
        {
            var timer = EnsureComp<ScreenTimerComponent>(uid);
            timer.Target = target.Value;
            timer.Priority = priority;
            if (target > _gameTiming.CurTime)
            {
                BuildTimerLayers(uid, timer, screen);
                DrawLayers(uid, screen, timer.LayerStatesToDraw);
            }
            else
            {
                OnTimerFinish(uid, screen);
                return;
            }
        }
        if (text != null)
        {
            BuildTextLayers(uid, screen, sprite);
            DrawLayers(uid, screen, screen.LayerStatesToDraw);
        }
    }

    /// <summary>
    ///     Removes the timer component, clears the sprite layer dict,
    ///     and draws <see cref="ScreenComponent.Text"/>
    /// </summary>
    private void OnTimerFinish(EntityUid uid, ScreenComponent screen)
    {
        if (!TryComp<ScreenTimerComponent>(uid, out var timer) || !TryComp<SpriteComponent>(uid, out var sprite))
            return;

        screen.Updates.Remove(timer.Priority);

        foreach (var key in timer.LayerStatesToDraw.Keys)
            sprite.RemoveLayer(key);

        RemComp<ScreenTimerComponent>(uid);

        ClearLayerStates(uid, screen, sprite);
        RefreshActiveUpdate(uid, screen);
    }

    /// <summary>
    ///     Word-wraps (and truncates) a string to a string?[] based on
    ///     <see cref="ScreenComponent.RowLength"/> and <see cref="ScreenComponent.Rows"/>.
    /// </summary>
    private string?[] SegmentText(string text, ScreenComponent component)
    {
        int segment = component.RowLength;
        var segmented = new string?[Math.Min(component.Rows, (text.Length - 1) / segment + 1)];

        // populate segmented with a string sliding window using Substring.
        // (Substring(5, 5) will return the 5 characters starting from 5th index)
        // the Mins are for the very short string case, the very long string case, and to not OOB the end of the string.
        for (int i = 0; i < Math.Min(text.Length, segment * component.Rows); i += segment)
            segmented[i / segment] = text.Substring(i, Math.Min(text.Length - i, segment)).Trim();

        return segmented;
    }

    /// <summary>
    ///     Clears <see cref="ScreenComponent.LayerStatesToDraw"/>, and instantiates new blank defaults.
    /// </summary>
    private void ClearLayerStates(EntityUid uid, ScreenComponent component, SpriteComponent sprite)
    {
        foreach (var key in component.LayerStatesToDraw.Keys)
            sprite.RemoveLayer(key);

        component.LayerStatesToDraw.Clear();

        for (var row = 0; row < component.Rows; row++)
            for (var i = 0; i < component.RowLength; i++)
            {
                var key = TextMapKey + row + i;
                sprite.LayerMapReserveBlank(key);
                component.LayerStatesToDraw.Add(key, null);
                sprite.LayerSetRSI(key, new ResPath(TextPath));
                sprite.LayerSetState(key, DefaultState);
            }
    }

    /// <summary>
    ///     Sets the states in the <see cref="ScreenComponent.LayerStatesToDraw"/> to match the
    ///     <see cref="ScreenComponent.ActiveUpdate.Text"/> string.
    /// </summary>
    private void BuildTextLayers(EntityUid uid, ScreenComponent component, SpriteComponent sprite)
    {
        if (component.ActiveUpdate == null || component.ActiveUpdate.Value.Text == null)
            return;

        var text = SegmentText(component.ActiveUpdate.Value.Text, component);

        // by rows and then columns
        for (var rowIdx = 0; rowIdx < Math.Min(text.Length, component.Rows); rowIdx++)
        {
            var row = text[rowIdx];
            if (row == null)
                continue;
            var min = Math.Min(row.Length, component.RowLength);

            for (var chr = 0; chr < min; chr++)
            {
                component.LayerStatesToDraw[TextMapKey + rowIdx + chr] = GetStateFromChar(row[chr]);
                sprite.LayerSetOffset(
                    TextMapKey + rowIdx + chr,
                    Vector2.Multiply(
                        new Vector2((chr - min / 2f + 0.5f) * CharWidth, -rowIdx * component.RowOffset),
                        ScreenComponent.PixelSize
                        ) + component.TextOffset
                );
            }
        }
    }

    /// <summary>
    ///     Populates timer.LayerStatesToDraw & the sprite component's layer dict with calculated offsets.
    /// </summary>
    private void BuildTimerLayers(EntityUid uid, ScreenTimerComponent timer, ScreenComponent screen)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        string time = TimeToString(
            (_gameTiming.CurTime - timer.Target).Duration(),
            false,
            screen.HourFormat, screen.MinuteFormat, screen.SecondFormat
            );

        int min = Math.Min(time.Length, screen.RowLength);

        for (int i = 0; i < min; i++)
        {
            timer.LayerStatesToDraw[TimerMapKey + i] = GetStateFromChar(time[i]);
            sprite.LayerSetOffset(
                TimerMapKey + i,
                Vector2.Multiply(
                    new Vector2((i - min / 2f + 0.5f) * CharWidth, 0f),
                    ScreenComponent.PixelSize
                    ) + screen.TimerOffset
            );
        }
    }

    /// <summary>
    ///     Draws a LayerStates dict by setting sprite states individually.
    /// </summary>
    private void DrawLayers(EntityUid uid, ScreenComponent screen, Dictionary<string, string?> layerStates, SpriteComponent? sprite = null)
    {
        if (!Resolve(uid, ref sprite))
            return;

        var update = screen.ActiveUpdate;
        var color = update != null && update.Value.Color != null ? update.Value.Color : screen.DefaultColor;

        foreach (var (key, state) in layerStates.Where(pairs => pairs.Value != null))
        {
            sprite.LayerSetState(key, state);
            sprite.LayerSetColor(key, color.Value);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ScreenTimerComponent, ScreenComponent>();
        while (query.MoveNext(out var uid, out var timer, out var screen))
        {
            if (timer.Target < _gameTiming.CurTime)
            {
                OnTimerFinish(uid, screen);
                continue;
            }

            BuildTimerLayers(uid, timer, screen);
            DrawLayers(uid, screen, timer.LayerStatesToDraw);
        }
    }

    /// <summary>
    ///     Returns the <paramref name="timeSpan"/> converted to a string in either HH:MM, MM:SS or potentially SS:mm format.
    /// </summary>
    /// <param name="timeSpan">TimeSpan to convert into string.</param>
    /// <param name="getMilliseconds">Should the string be ss:ms if minutes are less than 1?</param>
    /// <remarks>
    ///     hours, minutes, seconds, and centiseconds are each set to 2 decimal places by default.
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
    ///     Returns the Effects/text.rsi state string based on <paramref name="character"/>, or null if none available.
    /// </summary>
    public static string? GetStateFromChar(char? character)
    {
        if (character == null)
            return null;

        // First checks if its one of our special characters
        if (CharStatePairs.ContainsKey(character.Value))
            return CharStatePairs[character.Value];

        // Or else it checks if its a normal letter or digit
        if (char.IsLetterOrDigit(character.Value))
            return character.Value.ToString().ToLower();

        return null;
    }
}
