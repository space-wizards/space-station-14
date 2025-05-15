using Content.Shared.Clothing.ActionEvent;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Stealth.Components;
using Robust.Shared.Physics;
using Content.Shared.Coordinates;

namespace Content.Server.Clothing.Systems
{
    public sealed class SecurityHailerSystem : SharedSecurityHailerSystem
    {
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SecurityHailerComponent, ActionSecHailerActionEvent>(OnHailOrder);
        }

        private void OnHailOrder(Entity<SecurityHailerComponent> ent, ref ActionSecHailerActionEvent ev)
        {
            Log.Debug("OnHailOrder servers side reached !");

            //if (args.Handled)
            //    return;

            _chat.TrySendInGameICMessage(ev.Performer, "HALT!!", InGameICChatType.Speak, true, true, checkRadioPrefix: false);  //Speech that isn't sent to chat or adminlogs
            //Exclamation point code here so it can be predicted
            var (uid, comp) = ent;
            if (!Resolve(uid, ref comp, false) || comp.Distance <= 0)
                ev.Handled = false;

            ExclamateHumanoidsAround(ent);
            ev.Handled = true;
        }

        private void ExclamateHumanoidsAround(Entity<SecurityHailerComponent> ent)
        {
            var (uid, comp) = ent;
            StealthComponent? stealth = null;
            foreach (var iterator in
                _entityLookup.GetEntitiesInRange<HumanoidAppearanceComponent>(_transform.GetMapCoordinates(uid), comp.Distance))
            {
                //Avoid pinging invisible entities
                if (TryComp(iterator, out stealth) && stealth.Enabled)
                    continue;

                //We don't want to ping user of whistle
                if (iterator.Owner == uid)
                    continue;

                SpawnAttachedTo(comp.ExclamationEffect, iterator.Owner.ToCoordinates());
            }
        }
    }
}
