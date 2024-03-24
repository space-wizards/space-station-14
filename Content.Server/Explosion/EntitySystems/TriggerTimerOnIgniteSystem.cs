using Content.Shared.Explosion.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Temperature;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Explosion.Components;
using Robust.Shared.Random;
using Content.Shared.Trigger;
using Content.Server.IgnitionSource;


namespace Content.Server.Explosion
{
    public sealed class TriggerTimerOnIgniteSystem : EntitySystem
    {
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;



        public override void Initialize()
        {
            SubscribeLocalEvent<OnIgniteTimerTriggerComponent, InteractUsingEvent>(OnInteracted);
            SubscribeLocalEvent<RandomTimerTriggerComponent, MapInitEvent>(OnRandomTimerTriggerMapInit2);
            //SubscribeLocalEvent<OnIgniteTimerTriggerComponent, UseInHandEvent>(OnUseInHand);
        }

        private void OnInteracted(EntityUid uid, OnIgniteTimerTriggerComponent component, InteractUsingEvent args)
        {
            if (TryComp<IgnitionSourceComponent>(args.Used, out var comp)) {
                if(comp.Ignited == true)
                {
                    if (args.Handled)
                        return;

                    var active = AddComp<ActiveTimerTriggerComponent>(uid);
                    active.TimeRemaining = component.Delay;
                    active.User = args.User;
                    active.BeepSound = component.BeepSound;
                    active.BeepInterval = component.BeepInterval;
                    active.TimeUntilBeep = component.InitialBeepDelay ?? 0f;
                    active.TimeUntilBeep = component.InitialBeepDelay == null ? active.BeepInterval : component.InitialBeepDelay.Value;

                    var ev = new ActiveTimerTriggerEvent(uid, args.User);
                    RaiseLocalEvent(uid, ref ev);
                    _popupSystem.PopupEntity(Loc.GetString("trigger-activated", ("device", uid)), args.User, args.User);
                    Log.Debug("You used this with another item!");
                    var triggerEvent = new TriggerEvent(uid, args.User);

                    if (TryComp<AppearanceComponent>(uid, out var appearance))
                        _appearance.SetData(uid, TriggerVisuals.VisualState, TriggerVisualState.Primed, appearance);

                }
            }
        }

        private void OnUseInHand(EntityUid uid, OnIgniteTimerTriggerComponent component, UseInHandEvent args)
        {
            Log.Debug("You used this in your hand!");
        }
        private void OnRandomTimerTriggerMapInit2(Entity<RandomTimerTriggerComponent> ent, ref MapInitEvent args)
        {
            var (_, comp) = ent;

            if (!TryComp<OnIgniteTimerTriggerComponent>(ent, out var timerTriggerComp))
                return;

            timerTriggerComp.Delay = _random.NextFloat(comp.Min, comp.Max);
        }


    }
}
