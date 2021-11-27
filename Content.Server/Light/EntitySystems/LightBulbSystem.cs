using Content.Server.Light.Components;
using Content.Server.Light.Events;
using Content.Shared.Light;
using Content.Shared.Throwing;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Player;

namespace Content.Server.Light.EntitySystems
{
    public sealed class LightBulbSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<LightBulbComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<LightBulbComponent, LandEvent>(HandleLand);
        }

        private void OnInit(EntityUid uid, LightBulbComponent bulb, ComponentInit args)
        {
            // update default state of bulbs
            SetColor(uid, bulb.Color, bulb);
            SetState(uid, bulb.State, bulb);
        }

        private void HandleLand(EntityUid uid, LightBulbComponent bulb, LandEvent args)
        {
            PlayBreakSound(uid, bulb);
            SetState(uid, LightBulbState.Broken, bulb);
        }

        /// <summary>
        ///     Set a new color for a light bulb and raise event about change
        /// </summary>
        public void SetColor(EntityUid uid, Color color, LightBulbComponent? bulb = null)
        {
            if (!Resolve(uid, ref bulb))
                return;

            bulb.Color = color;
            UpdateAppearance(uid, bulb);
        }

        /// <summary>
        ///     Set a new state for a light bulb (broken, burned) and raise event about change
        /// </summary>
        public void SetState(EntityUid uid, LightBulbState state, LightBulbComponent? bulb = null)
        {
            if (!Resolve(uid, ref bulb))
                return;

            bulb.State = state;
            UpdateAppearance(uid, bulb);
        }

        public void PlayBreakSound(EntityUid uid, LightBulbComponent? bulb = null)
        {
            if (!Resolve(uid, ref bulb))
                return;

            SoundSystem.Play(Filter.Pvs(uid), bulb.BreakSound.GetSound(), uid);
        }

        private void UpdateAppearance(EntityUid uid, LightBulbComponent? bulb = null,
            AppearanceComponent? appearance = null)
        {
            if (!Resolve(uid, ref bulb, ref appearance, logMissing: false))
                return;

            // try to update appearance and color
            appearance.SetData(LightBulbVisuals.State, bulb.State);
            appearance.SetData(LightBulbVisuals.Color, bulb.Color);
        }
    }
}
