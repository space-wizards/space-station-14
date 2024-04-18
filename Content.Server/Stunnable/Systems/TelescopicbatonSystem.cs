using Content.Server.Stunnable.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage.Events;
using Content.Shared.Examine;
using Content.Shared.Item;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Popups;
using Content.Shared.Stunnable;

namespace Content.Server.Stunnable.Systems
{
    public sealed class TelescopicbatonSystem : SharedTelescopicbatonSystem
    {
        [Dependency] private readonly SharedItemSystem _item = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly SharedItemToggleSystem _itemToggle = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<TelescopicbatonComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<TelescopicbatonComponent, StaminaDamageOnHitAttemptEvent>(OnStaminaHitAttempt);
            SubscribeLocalEvent<TelescopicbatonComponent, ItemToggledEvent>(ToggleDone);
        }

        private void OnStaminaHitAttempt(Entity<TelescopicbatonComponent> entity, ref StaminaDamageOnHitAttemptEvent args)
        {
            if (!_itemToggle.IsActivated(entity.Owner))
            {
                args.Cancelled = true;
            }
        }

        private void OnExamined(Entity<TelescopicbatonComponent> entity, ref ExaminedEvent args)
        {
            var onMsg = _itemToggle.IsActivated(entity.Owner)
            ? Loc.GetString("comp-telescopicbaton-examined-on")
            : Loc.GetString("comp-telescopicbaton-examined-off");
            args.PushMarkup(onMsg);
        }

        private void ToggleDone(Entity<TelescopicbatonComponent> entity, ref ItemToggledEvent args)
        {
            _item.SetHeldPrefix(entity.Owner, args.Activated ? "on" : "off");
        }        
    }
}
