
using Content.Shared.Sanity.Components;
using Content.Shared.Popups;

namespace Content.Server.Sanity
{
    public sealed class SanitySystem : EntitySystem
    {

        [Dependency] private readonly SharedPopupSystem _popup = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SanityComponent, SanityEvent>(OnState);
        }
        private void OnState(EntityUid uid, SanityComponent component, ref SanityEvent args)
        {
            if (component.lvl < 100)
            {
                component.lvl += 1;
            }
            if (component.lvl > 67 && component.lvl < 100)
            {
                _popup.PopupEntity(Loc.GetString("Вы чувствуете головную боль"), uid, uid);
            }
            if (component.lvl <= 67 && component.lvl > 34)
            {
                _popup.PopupEntity(Loc.GetString("У вас болит голова, кости будто ломаются на части"), uid, uid);
            }
            if (component.lvl <= 34 && component.lvl > 0)
            {
                _popup.PopupEntity(Loc.GetString("Вы теряете рассудок, вам совмем плохо!"), uid, uid);
            }
        }


    }
}
