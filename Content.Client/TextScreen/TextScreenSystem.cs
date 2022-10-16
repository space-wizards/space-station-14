using Robust.Shared.Timing;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;
using Content.Shared.TextScreen;

namespace Content.Client.TextScreen
{

    public sealed partial class TextScreenSystem : VisualizerSystem<TextScreenVisualsComponent>
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

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

        private const string TextScreenLayerMapKey = "textScreenLayerMapKey";

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<TextScreenVisualsComponent, ComponentInit>(OnInit);
        }

        private void OnInit(EntityUid uid, TextScreenVisualsComponent component, ComponentInit args)
        {
            if (!TryComp(uid, out SpriteComponent? sprite))
                return;

            ResetTextLength(component, sprite);
            PrepareTextLayers(component, sprite);
            UpdateLayersToText(component, sprite);
        }

        /// <summary>
        ///     Resets all sprite layers, through removing them and then creating new ones.
        /// </summary>
        public void ResetTextLength(TextScreenVisualsComponent component, SpriteComponent? sprite = null)
        {
            if (!Resolve(component.Owner, ref sprite))
                return;

            foreach (var (key, _) in component.TextLayers)
                sprite.RemoveLayer(key);

            component.TextLayers.Clear();

            var length = component.TextLength;
            component.TextLength = 0;
            SetTextLength(component, length, sprite);
        }

        /// <summary>
        ///     Sets the text length, updating the sprite layers if necessary.
        /// </summary>
        public void SetTextLength(TextScreenVisualsComponent component, int newLength, SpriteComponent? sprite = null)
        {
            if (newLength == component.TextLength)
                return;

            if (!Resolve(component.Owner, ref sprite))
                return;

            var oldLength = component.TextLength;

            if (newLength > oldLength)
            {
                for (var i = oldLength; i < newLength; i++)
                {
                    sprite.LayerMapReserveBlank(TextScreenLayerMapKey + i);
                    component.TextLayers.Add(TextScreenLayerMapKey + i, null);
                    sprite.LayerSetRSI(TextScreenLayerMapKey + i, new ResourcePath("Effects/text.rsi"));
                    sprite.LayerSetColor(TextScreenLayerMapKey + i, component.Color);
                    sprite.LayerSetState(TextScreenLayerMapKey + i, "blank"); //set a default? maybe
                }
            }
            else
            {
                for (var i = oldLength; i > newLength; i--)
                {
                    sprite.LayerMapGet(TextScreenLayerMapKey + (i - 1));
                    component.TextLayers.Remove(TextScreenLayerMapKey + (i - 1));
                    sprite.RemoveLayer(TextScreenLayerMapKey + (i - 1));
                }
            }

            UpdateOffsets(component, sprite);

            component.TextLength = newLength;
        }

        /// <summary>
        ///     Updates the layers offsets based on the text length, so it is drawn correctly.
        /// </summary>
        public void UpdateOffsets(TextScreenVisualsComponent component, SpriteComponent? sprite = null)
        {
            if (!Resolve(component.Owner, ref sprite))
                return;

            for (var i = 0; i < component.TextLayers.Count; i++)
            {
                var offset = i - (component.TextLayers.Count - 1) / 2.0f;
                sprite.LayerSetOffset(TextScreenLayerMapKey + i, new Vector2(offset * TextScreenVisualsComponent.PixelSize * 4.0f, 0.0f) + component.TextOffset);
            }
        }

        protected override void OnAppearanceChange(EntityUid uid, TextScreenVisualsComponent component, ref AppearanceChangeEvent args)
        {
            UpdateAppearance(component, args.Component, args.Sprite);
        }

        public void UpdateAppearance(TextScreenVisualsComponent component, AppearanceComponent? appearance = null, SpriteComponent? sprite = null)
        {
            if (!Resolve(component.Owner, ref appearance, ref sprite))
                return;

            if (appearance.TryGetData(TextScreenVisuals.On, out bool on))
            {
                component.Activated = on;
                UpdateVisibility(component, sprite);
            }

            if (appearance.TryGetData(TextScreenVisuals.Mode, out TextScreenMode mode))
            {
                component.CurrentMode = mode;
                UpdateText(component);
            }

            if (appearance.TryGetData(TextScreenVisuals.TargetTime, out TimeSpan time))
            {
                component.TargetTime = time;
            }

            if (appearance.TryGetData(TextScreenVisuals.ScreenText, out string text))
            {
                component.Text = text;
                UpdateText(component);
            }

            PrepareTextLayers(component, sprite);
            UpdateLayersToText(component, sprite);
        }

        /// <summary>
        ///     If currently in <see cref="TextScreenMode.Text"/> mode: <br/>
        ///     Sets <see cref="TextScreenVisualsComponent.ShowText"/> to the value of <see cref="TextScreenVisualsComponent.Text"/>
        /// </summary>
        public static void UpdateText(TextScreenVisualsComponent component)
        {
            if (component.CurrentMode == TextScreenMode.Text)
                component.ShowText = component.Text;
        }

        /// <summary>
        ///     Updates visibility of text if the component is activated.
        /// </summary>
        public void UpdateVisibility(TextScreenVisualsComponent component, SpriteComponent? sprite = null)
        {
            if (!Resolve(component.Owner, ref sprite))
                return;

            foreach (var (key, _) in component.TextLayers)
            {
                sprite.LayerSetVisible(key, component.Activated);
            }
        }

        /// <summary>
        ///     Sets the text in the TextLayers to match the component ShowText string.
        /// </summary>
        /// <remarks>
        ///     Remember to set component.Text to a string first.
        ///     
        ///     Invalid characters turn into null, only the beginning of the string is handled.
        /// </remarks>
        public void PrepareTextLayers(TextScreenVisualsComponent component, SpriteComponent? sprite = null)
        {
            if (!Resolve(component.Owner, ref sprite))
                return;

            for (var i=0; (i<component.TextLength); i++)
            {
                if (i>=component.ShowText.Length)
                {
                    component.TextLayers[TextScreenLayerMapKey + i] = "blank";
                    continue;
                }
                component.TextLayers[TextScreenLayerMapKey + i] = GetStateFromChar(component.ShowText[i]);
            }
        }

        /// <summary>
        ///     Updates sprite layers to match text in the LayerText
        /// </summary>
        public void UpdateLayersToText(TextScreenVisualsComponent component, SpriteComponent? sprite = null)
        {
            if (!Resolve(component.Owner, ref sprite))
                return;

            foreach (var (key, state) in component.TextLayers)
            {
                if (state == null)
                    continue;
                sprite.LayerSetState(key, state);
            }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var comp in EntityQuery<TextScreenVisualsComponent>())
            {
                if (comp.CurrentMode == TextScreenMode.Timer)
                {
                    // Basically Abs(TimeSpan, TimeSpan) -> Gives the difference between the current time and the target time.
                    var timeToShow = _gameTiming.CurTime > comp.TargetTime ? _gameTiming.CurTime - comp.TargetTime : comp.TargetTime - _gameTiming.CurTime;
                    comp.ShowText = TimeToString(timeToShow, false);
                    PrepareTextLayers(comp);
                    UpdateLayersToText(comp);
                }
            }
        }

        /// <param name="timeSpan">TimeSpan to convert into string.</param>
        /// <param name="getMilliseconds">Should the string be ss:ms if minutes are less than 1?</param>
        /// <returns>Returns either HH:MM, MM:SS or potentially SS:MS based on the <paramref name="timeSpan"/>.</returns>
        public static string TimeToString(TimeSpan timeSpan, bool getMilliseconds = true)
        {
            string firstString;
            string lastString;

            if (timeSpan.TotalHours >= 1)
            {
                firstString = timeSpan.Hours.ToString("D2");
                lastString = timeSpan.Minutes.ToString("D2");
            }
            else if (timeSpan.TotalMinutes >= 1 || !getMilliseconds)
            {
                firstString = timeSpan.Minutes.ToString("D2");
                // It's nicer to see a timer set at 5 seconds actually start at 00:05 instead of 00:04, and when the timer reaches 0, it should be set off almost immediately.
                var seconds = timeSpan.Seconds + (timeSpan.Milliseconds > 200 ? 1 : 0);
                lastString = seconds.ToString("D2");
            }
            else
            {
                firstString = timeSpan.Seconds.ToString("D2");
                var centiseconds = timeSpan.Milliseconds / 10;
                lastString = centiseconds.ToString("D2");
            }

            return firstString + ':' + lastString;
        }

        /// <returns>Layer state string from <paramref name="character"/>, or null if none available</returns>
        public static string? GetStateFromChar(char? character)
        {
            if (character == null)
                return null;

            if (CharStatePairs.ContainsKey(character.Value))
                return CharStatePairs[character.Value];

            if (char.IsLetterOrDigit(character.Value))
                return character.Value.ToString().ToLower();

            return null;
        }

    }

}
