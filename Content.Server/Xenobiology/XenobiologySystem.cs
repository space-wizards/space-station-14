using Robust.Shared.GameObjects;
using Content.Server.Power.Components;
using Content.Shared.Examine;
using Robust.Shared.Localization;
using Content.Shared.Interaction;
using YamlDotNet.Core.Tokens;
using Content.Shared.Tag;
using Content.Shared.Notification.Managers;
using Content.Server.Notification;

namespace Content.Server.Xenobiology
{
    public class XenobiologySystem : EntitySystem
    {

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SpecimenContainmentComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<SpecimenDietComponent, ExaminedEvent>(OnSpecimenExamined);
            SubscribeLocalEvent<SpecimenDietComponent, InteractUsingEvent>(OnSpecimenFed);
        }

        private void OnSpecimenExamined(EntityUid uid, SpecimenDietComponent component, ExaminedEvent args)
        {
            args.Message.AddText("\n");
            args.Message.AddMarkup(Loc.GetString("specimen-hungry") + (" "));
            args.Message.AddMarkup($"[color=#f6ff05]{component.SelectedDiet}[/color]");
        }

        private void OnSpecimenFed(EntityUid uid, SpecimenDietComponent comp, InteractUsingEvent args)
        {
           var specimen = args.Used.EntityManager.GetEntity(uid);
           var usedItem = args.Used;
           //If the usedItem has a certian tag, which corresponds to the picked diet, we increase the growth state
           //Otherwise, output a message about the wrong food choice
           if (usedItem.TryGetComponent<TagComponent>(out TagComponent? ItemTag))
           {
                if (ItemTag.Tags.Contains(comp.SelectedDiet))
                {
                    comp.GrowthState++;
                    specimen.PopupMessageEveryone(Loc.GetString("specimen-humanlike-fed"));
                    args.Used.EntityManager.DeleteEntity(args.Used); //Remove the food
                    comp.SelectDiet(); //Set a new random diet
                }
                else
                {
                    specimen.PopupMessageEveryone(Loc.GetString("specimen-wrong-food"));
                }
            }
        }

        private void OnPowerChanged(EntityUid uid, SpecimenContainmentComponent component, PowerChangedEvent args)
        {
            component.Powered = args.Powered;
            component.UpdateAppearance();
        } 
    }
}
