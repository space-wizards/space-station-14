using System.Linq;
using Content.Shared.Actions;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Player;
using Content.Shared.Trigger;
using Content.Shared.Database;
using Content.Shared.Explosion;
using Content.Shared.Interaction;
using Content.Shared.Payload.Components;
using Content.Shared.StepTrigger.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;
using Robust.Client.GameObjects;
using Content.Shared.MachineLinking;
using Robust.Shared.Utility;
using Robust.Client.Utility;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using System;
using TerraFX.Interop.Windows;
using Prometheus;
using Content.Client.MachineLinking;
using Content.Shared.TextScreen;

namespace Content.Client.TextScreen
{

    public sealed partial class TextScreenSystem : VisualizerSystem<TextScreenVisualsComponent>
    {
        [Dependency] private readonly SharedBroadphaseSystem _broadphase = default!;
        [Dependency] private readonly SharedContainerSystem _container = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

        private const string LayerMapPrefix = "textScreen";

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<TextScreenVisualsComponent, ComponentInit>(OnInit);
        }

        private void OnInit(EntityUid uid, TextScreenVisualsComponent component, ComponentInit args)
        {
            if (!TryComp(uid, out SpriteComponent? sprite))
                return;

            sprite.LayerMapReserveBlank(TextScreenVisualLayers.Screen);
            sprite.LayerSetRSI(TextScreenVisualLayers.Screen, new ResourcePath("Effects/text.rsi"));
            sprite.LayerSetState(TextScreenVisualLayers.Screen, "screen");

            ResetTextLength(component, sprite);
            PrepareTextLayers(component, sprite);
            UpdateLayersToText(component, sprite);
        }

        /// <summary>
        ///     Resets all sprite layers, and sets them back again
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
                    sprite.LayerMapReserveBlank(LayerMapPrefix + i);
                    component.TextLayers.Add(LayerMapPrefix + i, null);
                    sprite.LayerSetRSI(LayerMapPrefix + i, new ResourcePath("Effects/text.rsi"));
                    sprite.LayerSetColor(LayerMapPrefix + i, component.Color);
                    sprite.LayerSetState(LayerMapPrefix + i, "blank");
                }
            }
            else
            {
                for (var i = oldLength; i > newLength; i--)
                {
                    sprite.LayerMapGet(LayerMapPrefix + (i - 1));
                    component.TextLayers.Remove(LayerMapPrefix + (i - 1));
                    sprite.RemoveLayer(LayerMapPrefix + (i - 1));
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
                sprite.LayerSetOffset(LayerMapPrefix + i, new Vector2(offset * TextScreenVisualsComponent.PixelSize * 4.0f, 0.0f));
            }
        }

        // TODO: cleanup needed
        protected override void OnAppearanceChange(EntityUid uid, TextScreenVisualsComponent component, ref AppearanceChangeEvent args)
        {
            UpdateAppearance(component, args.Component, args.Sprite);
        }

        public void UpdateText(TextScreenVisualsComponent component)
        {
            if (component.CurrentMode == TextScreenMode.Text)
                component.ShowText = component.Text;
        }

        // TODO: cleanup needed
        public void UpdateAppearance(TextScreenVisualsComponent component, AppearanceComponent? appearance = null, SpriteComponent? sprite = null)
        {
            if (!Resolve(component.Owner, ref appearance, ref sprite))
                return;

            if (appearance.TryGetData(TextScreenVisuals.On, out bool on))
            {
                component.Activated = on;
                UpdateVisibility(component, sprite); // needs to based on activated let text be visible or not
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

            var rsi = sprite.LayerGetActualRSI(TextScreenVisualLayers.Screen);

            if (rsi == null)
                return;

            for (var i=0; (i<component.TextLength); i++)
            {
                if (i>=component.ShowText.Length)
                {
                    component.TextLayers[LayerMapPrefix + i] = "blank";
                    continue;
                }
                component.TextLayers[LayerMapPrefix + i] = GetLayerStateFromChar(component.ShowText[i], rsi);
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
                    TimeSpan timeToShow = _gameTiming.CurTime > comp.TargetTime ? _gameTiming.CurTime - comp.TargetTime : comp.TargetTime - _gameTiming.CurTime;
                    comp.ShowText = TimeToString(timeToShow);
                    PrepareTextLayers(comp);
                    UpdateLayersToText(comp);
                }
            }
        }

        public static string TimeToString(TimeSpan timeSpan)
        {
            string firstString;
            string lastString;

            if (timeSpan.TotalHours >= 1)
            {
                firstString = timeSpan.Hours.ToString("D2");
                lastString = timeSpan.Minutes.ToString("D2");
            }
            else if (timeSpan.TotalMinutes >= 1)
            {
                firstString = timeSpan.Minutes.ToString("D2");
                lastString = timeSpan.Seconds.ToString("D2");
            }
            else
            {
                firstString = timeSpan.Seconds.ToString("D2");
                lastString = timeSpan.Milliseconds.ToString("D2");
            }

            return firstString + ":" + lastString;
        }

        public string? GetLayerStateFromChar(char? character, RSI rsi)
        {
            if (character == null)
                return null;

            //TODO: change to dictionary lookup
            switch (character)
            {
                case ':':
                    return "colon";
                case '!':
                    return "exclamation";
                case '?':
                    return "question";
                case '*':
                    return "star";
                case '+':
                    return "plus";
                case ' ':
                    return "blank";
            }
            var stringChar = character.ToString();

            if (stringChar != null)
                stringChar = stringChar.ToLower();

            if (rsi.TryGetState(stringChar, out _))
                return stringChar;

            return null;
        }

        /*
        private void UpdateTimer()
        {
            foreach (var timer in EntityQuery<SignalTimerVisualsComponent>())
            {
                if (!timer.Activated)
                    continue;

                if (timer.TriggerTime <= _gameTiming.CurTime)
                {
                    Trigger(timer.Owner, timer);

                    if (timer.DoneSound != null)
                    {
                        var filter = Filter.Pvs(timer.Owner, entityManager: EntityManager);
                        _audio.Play(timer.DoneSound, filter, timer.Owner, timer.BeepParams);
                    }
                }
            }
        }
        */
    }

    public enum TextScreenVisualLayers : byte
    {
        Screen,
    }
}
