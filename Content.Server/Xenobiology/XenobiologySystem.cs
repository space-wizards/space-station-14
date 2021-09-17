using Robust.Shared.GameObjects;
using Content.Server.Power.Components;
using Content.Shared.Examine;
using Robust.Shared.Localization;
using Content.Shared.Interaction;
using YamlDotNet.Core.Tokens;
using Content.Shared.Tag;
using Content.Shared.Notification.Managers;
using Content.Server.Notification;
using System;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Content.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Random;

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
        private readonly IRobustRandom _random = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SpecimenContainmentComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<SpecimenContainmentComponent, InteractUsingEvent>(OnTubeFeed);
            SubscribeLocalEvent<SpecimenContainmentComponent, ExaminedEvent>(OnContainmentExamined);
            SubscribeLocalEvent<SpecimenDietComponent, ComponentInit>(Initialize);
            SubscribeLocalEvent<SpecimenDietComponent, ExaminedEvent>(OnSpecimenExamined);
            SubscribeLocalEvent<SpecimenDietComponent, FeedEvent>(OnSpecimenFed);
        }

        /// <summary>
        /// Called when the specimen containment loses power
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="component"></param>
        /// <param name="args"></param>
        private void OnPowerChanged(EntityUid uid, SpecimenContainmentComponent comp, PowerChangedEvent args)
        {
            comp.UpdateAppearance();
        }

        private void Initialize(EntityUid uid, SpecimenDietComponent comp, ComponentInit args)
        {
            comp.SelectedDiet = _random.Pick(comp.DietPick);
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
                    comp.SelectedDiet = _random.Pick(comp.DietPick); //Pick a new random diet from the component
                    comp.GrowthState++;
                    if (comp.GrowthState >= 5)
                    {
                        specimen.TryRemoveFromContainer();
                    }
                }
                else
                {
                    specimen.PopupMessage(Loc.GetString("specimen-wrong-food"));
                }
               
            }
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
                    comp.UpdateAppearance();
                }
            }
        }

        /// <summary>
        /// Relays the information from the specimen diet to the containment component
        /// To show information in it's markup to the user
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="component"></param>
        /// <param name="args"></param>
        private void OnContainmentExamined(EntityUid uid, SpecimenContainmentComponent component, ExaminedEvent args)
        {
            //First, we take care of showing if the containment has someone in it
            if (component.TubeContainer.ContainedEntity == null)
            {
                args.Message.AddText("\n");
                args.Message.AddMarkup($"[color=#35f131]{Loc.GetString("specimen-growth-empty")}[/color]");
            }
            else if (component.TubeContainer.ContainedEntity != null)
            {
                args.Message.AddText("\n");
                args.Message.AddMarkup($"[color=#35f131]{Loc.GetString("specimen-growth-embryo")}[/color]");
                if (component.TubeContainer.ContainedEntity.TryGetComponent(out SpecimenDietComponent? dietcomp))
                {
                    args.Message.AddText("\n");
                    args.Message.AddMarkup(Loc.GetString("specimen-hungry") + (" "));
                    args.Message.AddMarkup($"[color=#f6ff05]{dietcomp.SelectedDiet}[/color]");
                }
            }
            
        }
    }
}
