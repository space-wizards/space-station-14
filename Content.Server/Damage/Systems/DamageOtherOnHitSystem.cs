using Content.Server.Administration.Logs;
using Content.Server.Damage.Components;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Camera;
using Content.Shared.Damage;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Effects;
using Content.Shared.Mobs.Components;
using Content.Shared.Throwing;
using Content.Shared.Wires;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Content.Shared.Popups;
using Content.Shared.Damage.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Utility;

namespace Content.Server.Damage.Systems
{
    public sealed class DamageOtherOnHitSystem : SharedDamageOtherOnHitSystem
    {
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly GunSystem _guns = default!;
        [Dependency] private readonly DamageableSystem _damageable = default!;
        [Dependency] private readonly DamageExamineSystem _damageExamine = default!;
        [Dependency] private readonly SharedCameraRecoilSystem _sharedCameraRecoil = default!;
        [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<StaminaComponent, BeforeThrowEvent>(OnBeforeThrow);
            SubscribeLocalEvent<DamageOtherOnHitComponent, DamageExamineEvent>(OnDamageExamine);
        }

        private void OnBeforeThrow(EntityUid uid, StaminaComponent component, ref BeforeThrowEvent args)
        {
            if (!TryComp<DamageOtherOnHitComponent>(args.ItemUid, out var damage))
                return;

            if (component.CritThreshold - component.StaminaDamage <= damage.StaminaCost)
            {
                args.Cancelled = true;
                _popup.PopupEntity(Loc.GetString("throw-no-stamina", ("item", args.ItemUid)), uid, uid);
                return;
            }
        }

        private void OnDamageExamine(EntityUid uid, DamageOtherOnHitComponent component, ref DamageExamineEvent args)
        {
            _damageExamine.AddDamageExamine(args.Message, GetDamage(uid, component, args.User), Loc.GetString("damage-throw"));

            if (component.StaminaCost == 0)
                return;

            var staminaCostMarkup = FormattedMessage.FromMarkupOrThrow(
                Loc.GetString("damage-stamina-cost",
                ("type", Loc.GetString("damage-throw")), ("cost", component.StaminaCost)));
            args.Message.PushNewline();
            args.Message.AddMessage(staminaCostMarkup);
        }
    }
}
