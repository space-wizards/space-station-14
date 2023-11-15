using System.Linq;
using System.Numerics;
using Content.Shared.TextScreen;
using Content.Shared.TextScreen.Components;
using Content.Shared.TextScreen.Events;
using FastAccessors;
using Robust.Client.GameObjects;
using Robust.Client.State;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using YamlDotNet.Core.Tokens;

namespace Content.Client.TextScreen;

/// <summary>
///     The TextScreenSystem draws text in the game world using 3x5 sprite states for each character.
/// </summary>
public sealed class TextScreenSystem : VisualizerSystem<TextScreenVisualsComponent>
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
    private const string TimerMapKey = "timerMapKey";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TextScreenVisualsComponent, ComponentInit>(OnTextInit);
        SubscribeLocalEvent<TextScreenTimerComponent, ComponentInit>(OnTimerInit);
    }

    private void OnTextInit(EntityUid uid, TextScreenVisualsComponent component, ComponentInit args)
    {
        if (!TryComp(uid, out SpriteComponent? sprite))
            return;

        component.TextOffset = Vector2.Multiply(TextScreenVisualsComponent.PixelSize, component.TextOffset);
        ResetTextLength(uid, component, sprite);
        BuildTextLayerStates(uid, component, sprite);
    }

    private void OnTimerInit(EntityUid uid, TextScreenTimerComponent component, ComponentInit args)
    {
        if (!TryComp(uid, out SpriteComponent? sprite) || !TryComp<TextScreenVisualsComponent>(uid, out var screen))
            return;

        for (var i = 0; i < screen.RowLength; i++)
        {
            sprite.LayerMapReserveBlank(TimerMapKey + i);
            component.LayerStatesToDraw.Add(TimerMapKey + i, null);
            sprite.LayerSetRSI(TimerMapKey + i, new ResPath("Effects/text.rsi"));
            sprite.LayerSetColor(TimerMapKey + i, screen.Color);
            sprite.LayerSetState(TimerMapKey + i, DefaultState);
        }
    }

    protected override void OnAppearanceChange(EntityUid uid, TextScreenVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (!Resolve(uid, ref args.Sprite))
            return;

        var appearance = args.Component;
        var sprite = args.Sprite;

        if (AppearanceSystem.TryGetData(uid, TextScreenVisuals.On, out bool on, appearance))
        {
            component.Activated = on;
            UpdateVisibility(uid, component, sprite);
        }

        if (AppearanceSystem.TryGetData(uid, TextScreenVisuals.ScreenText, out string?[] text, appearance))
        {
            component.TextToDraw = text;
        }

        if (AppearanceSystem.TryGetData(uid, TextScreenVisuals.TargetTime, out TimeSpan time, appearance))
        {
            var timer = EnsureComp<TextScreenTimerComponent>(uid);
            timer.Target = time;
            BuildTimerLayerStates(uid, timer, timer.Row * component.RowOffset);
            DrawLayerStates(uid, timer.LayerStatesToDraw);
        }

        BuildTextLayerStates(uid, component, sprite);
        DrawLayerStates(uid, component.LayerStatesToDraw);
        // UpdateLayersToDraw(uid, component, sprite);
    }

    /// <summary>
    ///     Sets visibility of text to <see cref="TextScreenVisualsComponent.Activated"/>.
    /// </summary>
    public void UpdateVisibility(EntityUid uid, TextScreenVisualsComponent component, SpriteComponent? sprite = null)
    {
        if (!Resolve(uid, ref sprite))
            return;

        var dict = TryComp<TextScreenTimerComponent>(uid, out var timer) ?
            component.LayerStatesToDraw.Concat(timer.LayerStatesToDraw) : component.LayerStatesToDraw;

        foreach (var (key, _) in dict)
        {
            sprite.LayerSetVisible(key, component.Activated);
        }
    }

    /// <summary>
    ///     Resets all TextScreenComponent sprite layers, through removing them and then creating new ones.
    /// </summary>
    public void ResetTextLength(EntityUid uid, TextScreenVisualsComponent component, SpriteComponent? sprite = null)
    {
        if (!Resolve(uid, ref sprite))
            return;

        foreach (var (key, _) in component.LayerStatesToDraw)
        {
            sprite.RemoveLayer(key);
        }

        component.LayerStatesToDraw.Clear();

        var length = component.RowLength;
        component.RowLength = 0;
        SetRowLength(uid, component, length, sprite);
    }

    /// <summary>
    ///     Sets <see cref="TextScreenVisualsComponent.RowLength"/>, adding or removing sprite layers if necessary.
    /// </summary>
    public void SetRowLength(EntityUid uid, TextScreenVisualsComponent component, int newLength, SpriteComponent? sprite = null)
    {
        if (newLength == component.RowLength)
            return;

        if (!Resolve(uid, ref sprite))
            return;

        if (newLength > component.RowLength)
        {
            for (var i = component.RowLength; i < newLength; i++)
            {
                sprite.LayerMapReserveBlank(TextMapKey + i);
                component.LayerStatesToDraw.Add(TextMapKey + i, null);
                sprite.LayerSetRSI(TextMapKey + i, new ResPath("Effects/text.rsi"));
                sprite.LayerSetColor(TextMapKey + i, component.Color);
                sprite.LayerSetState(TextMapKey + i, DefaultState);
            }
        }
        else
        {
            for (var i = component.RowLength; i > newLength; i--)
            {
                sprite.LayerMapGet(TextMapKey + (i - 1));
                component.LayerStatesToDraw.Remove(TextMapKey + (i - 1));
                sprite.RemoveLayer(TextMapKey + (i - 1));
            }
        }

        UpdateOffsets(uid, component, sprite);

        component.RowLength = newLength;
    }

    /// <summary>
    ///     Updates the layers' offsets based on text length.
    /// </summary>
    public void UpdateOffsets(EntityUid uid, TextScreenVisualsComponent component, SpriteComponent? sprite = null)
    {
        if (!Resolve(uid, ref sprite))
            return;

        int position = 0;
        for (var height = 0; height < Math.Min(component.Rows, component.TextToDraw.Length); height++)
        {
            var row = component.TextToDraw[height];
            if (row == null)
                continue;
            var rowLength = row.Length;

            for (var chr = 0; chr < rowLength; chr++)
            {
                var width = chr - (rowLength - 1) / 2.0f;
                sprite.LayerSetOffset(
                    TextMapKey + position,
                    new Vector2(
                        width * TextScreenVisualsComponent.PixelSize * 4f,
                        height * component.RowOffset
                        ) + component.TextOffset
                    );
                position++;
            }
        }
    }

    /// <summary>
    ///     If currently in <see cref="TextScreenMode.Text"/> mode: <br/>
    ///     Sets <see cref="TextScreenVisualsComponent.TextToDraw"/> to the value of <see cref="TextScreenVisualsComponent.Text"/>
    /// </summary>
    // public static void UpdateText(TextScreenVisualsComponent component)
    // {
    //     component.TextToDraw = component.Text;
    // }

    /// <summary>
    ///     Sets the states in the <see cref="TextScreenVisualsComponent.LayerStatesToDraw"/> to match the component <see cref="TextScreenVisualsComponent.TextToDraw"/> string.
    /// </summary>
    /// <remarks>
    ///     Remember to set <see cref="TextScreenVisualsComponent.TextToDraw"/> to a string first.
    /// </remarks>
    public void BuildTextLayerStates(EntityUid uid, TextScreenVisualsComponent component, SpriteComponent? sprite = null)
    {
        if (!Resolve(uid, ref sprite))
            return;

        int position = 0;
        for (var rowIdx = 0; rowIdx < Math.Min(component.TextToDraw.Length, component.Rows); rowIdx++)
        {
            var row = component.TextToDraw[rowIdx];
            if (row == null)
                continue;
            for (var chr = 0; chr < Math.Min(row.Length, component.RowLength); chr++)
            {
                // if (i >= component.TextToDraw.Length)
                // {
                //     component.LayerStatesToDraw[TextMapKey + i] = DefaultState;
                //     continue;
                // }
                component.LayerStatesToDraw[TextMapKey + position] = GetStateFromChar(row[chr]);
                sprite.LayerSetOffset(
                    TextMapKey + position,
                    Vector2.Multiply(new Vector2((chr - (row.Length - 1)) * 2, rowIdx), TextScreenVisualsComponent.PixelSize)
                );
                position++;
            }
        }
    }

    // public void SetTextOffsets(
    //     EntityUid uid,
    //     TextScreenVisualsComponent component,
    //     string chars,
    //     SpriteComponent? sprite = null,
    //     float vertOffset = 0
    //     )
    // {
    //     if (!Resolve(uid, ref sprite))
    //         return;

    //     for (int i = 0; i < chars.Length; i++)
    //     {
    //         component.LayerStatesToDraw[TextMapKey + i] = GetStateFromChar(chars[i]);
    //         sprite.LayerSetOffset(
    //             TextMapKey + i,
    //             Vector2.Multiply(new Vector2((i - (chars.Length - 1)) * 2, vertOffset), TextScreenVisualsComponent.PixelSize)
    //         );
    //     }
    // }

    public void BuildTimerLayerStates(EntityUid uid, TextScreenTimerComponent component, float vertOffset, SpriteComponent? sprite = null)
    {
        if (!Resolve(uid, ref sprite))
            return;

        string time = TimeToString(
            (_gameTiming.CurTime - component.Target).Duration(),
            false,
            component.HourFormat, component.MinuteFormat, component.SecondFormat
            );

        for (int i = 0; i < time.Length; i++)
        {
            component.LayerStatesToDraw[TimerMapKey + i] = GetStateFromChar(time[i]);
            sprite.LayerSetOffset(
                TimerMapKey + i,
                Vector2.Multiply(new Vector2((i - (time.Length - 1)) * 2, vertOffset), TextScreenVisualsComponent.PixelSize)
            );
        }
    }

    private void DrawLayerStates(EntityUid uid, Dictionary<string, string?> layerStates, SpriteComponent? sprite = null)
    {
        if (!Resolve(uid, ref sprite))
            return;

        foreach (var (key, state) in layerStates.Where(pairs => pairs.Value != null))
            sprite.LayerSetState(key, state);
    }

    // private void BuildTimerLayers(
    //     EntityUid uid,
    //     TextScreenTimerComponent timer,
    //     SpriteComponent? sprite = null
    // )
    // {
    //     if (!Resolve(uid, ref sprite) || !TryComp<TextScreenVisualsComponent>(uid, out var screen))
    //         return;

    //     int position = timer.Row * screen.RowLength;
    //     for (int i = position; i < position + screen.RowLength; i++)


    // }

    /// <summary>
    ///     Iterates through <see cref="TextScreenVisualsComponent.LayerStatesToDraw"/>, setting sprite states to the appropriate layers.
    /// </summary>
    // public void UpdateLayersToDraw(EntityUid uid, TextScreenVisualsComponent component, SpriteComponent? sprite = null)
    // {
    //     if (!Resolve(uid, ref sprite))
    //         return;

    //     foreach (var (key, state) in component.LayerStatesToDraw)
    //     {
    //         if (state == null)
    //             continue;
    //         sprite.LayerSetState(key, state);
    //     }
    // }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TextScreenTimerComponent, TextScreenVisualsComponent>();
        while (query.MoveNext(out var uid, out var timer, out var comp))
        {
            // comp.TextToDraw[timer.Row] = TimeToString(timeToShow, false, minutes: timer.MinuteFormat);
            // BuildTextLayerStates(uid, comp);
            // UpdateLayersToDraw(uid, comp);
            BuildTimerLayerStates(uid, timer, timer.Row * comp.RowOffset);
            DrawLayerStates(uid, timer.LayerStatesToDraw);
        }
    }

    /// <summary>
    ///     Returns the <paramref name="timeSpan"/> converted to a string in either HH:MM, MM:SS or potentially SS:mm format.
    /// </summary>
    /// <param name="timeSpan">TimeSpan to convert into string.</param>
    /// <param name="getMilliseconds">Should the string be ss:ms if minutes are less than 1?</param>
    public static string TimeToString(TimeSpan timeSpan, bool getMilliseconds = true, string hours = "D2", string minutes = "D2", string seconds = "D2")
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
            lastString = centiseconds.ToString("D2");
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
