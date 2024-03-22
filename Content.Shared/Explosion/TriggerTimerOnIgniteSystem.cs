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

namespace Content.Shared.Explosion
{
    public sealed class TriggerTimerOnIgniteSystem : EntitySystem
    {
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<OnIgniteTimerTriggerComponent, InteractUsingEvent>(OnInteracted);
            ;
            SubscribeLocalEvent<OnIgniteTimerTriggerComponent, UseInHandEvent>(OnUseInHand);
        }

        private void OnInteracted(EntityUid uid, OnIgniteTimerTriggerComponent component, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            _popupSystem.PopupEntity(Loc.GetString("trigger-activated", ("device", uid)), args.User, args.User);
            Log.Debug("You used this with another item!");
            var triggerEvent = new TriggerEvent(trigger, user);
            EntityManager.EventBus.RaiseLocalEvent(triggerEvent, triggerEvent, true);
            return triggerEvent.Handled;

        }

        private void OnUseInHand(EntityUid uid, OnIgniteTimerTriggerComponent component, UseInHandEvent args)
        {
            Log.Debug("You used this in your hand!");
        }


    }
}
