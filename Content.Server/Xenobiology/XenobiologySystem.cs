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
    public class FeedEvent : EntityEventArgs
    {
        public IEntity? FoodItem;

        public FeedEvent(IEntity item)
        {
            FoodItem = item;
        }
    }

    public class XenobiologySystem : EntitySystem
    {

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SpecimenContainmentComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<SpecimenContainmentComponent, InteractUsingEvent>(OnTubeFeed);
            SubscribeLocalEvent<SpecimenDietComponent, ExaminedEvent>(OnSpecimenExamined);
            SubscribeLocalEvent<SpecimenDietComponent, FeedEvent>(OnSpecimenFed);
        }

        /// <summary>
        /// Builds a dynamic specimen examination
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="component"></param>
        /// <param name="args"></param>
        private void OnSpecimenExamined(EntityUid uid, SpecimenDietComponent component, ExaminedEvent args)
        {
            args.Message.AddText("\n");
            args.Message.AddMarkup(Loc.GetString("specimen-hungry") + (" "));
            args.Message.AddMarkup($"[color=#f6ff05]{component.SelectedDiet}[/color]");
        }

        /// <summary>
        /// Occurs when you feed the specimen a specific item
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="comp"></param>
        /// <param name="args"></param>
        public void OnSpecimenFed(EntityUid uid, SpecimenDietComponent comp, FeedEvent args)
        {
           var specimen = comp.Owner;
           var usedItem = args.FoodItem;
           //If the usedItem has a certian tag, which corresponds to the picked diet, we increase the growth state
           //Otherwise, output a message about the wrong food choice
           if (usedItem == null) return;
           if (usedItem.TryGetComponent<TagComponent>(out TagComponent? ItemTag))
           {
                if (ItemTag.Tags.Contains(comp.SelectedDiet))
                {
                    specimen.PopupMessageEveryone(Loc.GetString("specimen-humanlike-fed"));
                    EntityManager.QueueDeleteEntity(usedItem.Uid); //Delete the food
                    comp.SelectDiet(); //Set a new random diet
                    comp.SpecimenGrow(); //Advance the growth state
                }
                else
                {
                    specimen.PopupMessageEveryone(Loc.GetString("specimen-wrong-food"));
                }
            }
        }

        /// <summary>
        /// Called when the power current is cut or restored
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="component"></param>
        /// <param name="args"></param>
        private void OnPowerChanged(EntityUid uid, SpecimenContainmentComponent component, PowerChangedEvent args)
        {
            component.Powered = args.Powered;
            component.UpdateAppearance();
        }

        /// <summary>
        /// If used on a containment chamber re-route the item input to raise a FeedEvent
        /// Which in turn goes through the handeling of said event
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="comp"></param>
        /// <param name="args"></param>
        public void OnTubeFeed(EntityUid uid, SpecimenContainmentComponent comp, InteractUsingEvent args)
        {
            if (comp.TubeContainer.ContainedEntity != null)
            {
                if (comp.TubeContainer.ContainedEntity.TryGetComponent(out SpecimenDietComponent? dietcomp))
                {
                    RaiseLocalEvent(comp.TubeContainer.ContainedEntity.Uid, new FeedEvent(args.Used));
                }
            }
        }
    }
}
