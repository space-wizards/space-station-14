using Robust.Shared.GameObjects;
using Content.Server.Power.Components;
using Content.Shared.Examine;
using Robust.Shared.Localization;

namespace Content.Server.Xenobiology
{
    public class XenobiologyTubeSystem : EntitySystem
    {

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SpecimenContainmentComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<SpecimenDietComponent, ExaminedEvent>(OnSpecimenExamined);
        }

        private void OnSpecimenExamined(EntityUid uid, SpecimenDietComponent component, ExaminedEvent args)
        {
            args.Message.AddText("\n");
            args.Message.AddMarkup(Loc.GetString("specimen-growth-hungry") + (" "));
            args.Message.AddMarkup($"[color=#f6ff05]{component.SelectedDiet}[/color]");
        }

        private void OnPowerChanged(EntityUid uid, SpecimenContainmentComponent component, PowerChangedEvent args)
        {
            component.Powered = args.Powered;
            component.UpdateAppearance();
        } 
    }
}
