using Content.Server.Popups;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using System;

namespace Content.Server.TimerStopwatch
{
    //NOTE: If named "stopwatch" causes conflicts with the engine stopwatch system
    public class StopwatchSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<TimerStopwatchComponent, UseInHandEvent>(OnUseInHand);
            SubscribeLocalEvent<TimerStopwatchComponent, GetInteractionVerbsEvent>(AddInteractionVerbsVerbs);
        }

        private void AddInteractionVerbsVerbs(EntityUid user, TimerStopwatchComponent component, GetInteractionVerbsEvent args)
        {
            //TODO: Add verb icons
            Verb trackverb = new();
            trackverb.Act = () => component.TrackingStatus = !component.TrackingStatus;
            trackverb.Text = Loc.GetString("timekeeping-verb-toggle");
            // trackverb.IconTexture = "/Textures/Interface/VerbIcons/eject.svg.192dpi.png";
            Verb resetverb = new();
            resetverb.Act = () => component.PassedTime = 0;
            resetverb.Text = Loc.GetString("timekeeping-verb-reset");
            // resetverb.IconTexture = "/Textures/Interface/VerbIcons/eject.svg.192dpi.png";
            args.Verbs.Add(trackverb);
            args.Verbs.Add(resetverb);
        }

        /// <summary>
        /// Interaction with the clock, tells the time.
        /// </summary>
        private void OnUseInHand(EntityUid uid, TimerStopwatchComponent comp, UseInHandEvent args)
        {
            TellTime(comp.PassedTime, args.User);
        }

        /// <summary>
        /// Tells the time of a given timekeeping device
        /// </summary>
        public void TellTime(float rawtime, EntityUid user)
        {
            var timeSpan = TimeSpan.FromSeconds(rawtime);
            _popupSystem.PopupEntity(timeSpan.Minutes.ToString() + ":" + timeSpan.Seconds.ToString(), user, Filter.Entities(user));
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            var comps = EntityManager.EntityQuery<TimerStopwatchComponent>();
            foreach(TimerStopwatchComponent comp in comps)
            {
                if(comp.TrackingStatus == true)
                {
                    comp.PassedTime += frameTime;
                }
            }
        }
    }
}
