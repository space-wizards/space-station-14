using Content.Shared.Emag.Components;
using Content.Shared.Examine;

namespace Content.Shared.Emag.Systems
{
    public sealed class SharedEmagFixerSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<EmagFixerComponent, ExaminedEvent>(OnExamine);
        }

        private void OnExamine(EntityUid uid, EmagFixerComponent component, ExaminedEvent args)
        {
            args.PushMarkup(Loc.GetString("emag-charges-remaining", ("charges", component.Charges)));
            if (component.Charges == component.MaxCharges)
            {
                args.PushMarkup(Loc.GetString("emag-max-charges"));
                return;
            }
        }
    }
}
