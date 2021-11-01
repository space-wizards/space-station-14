using Content.Shared.Examine;
using Content.Shared.Interaction;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Content.Shared.Hands.Components;
using Content.Shared.Popups;
using Content.Server.Tools;
using Content.Shared.Item;
using Content.Shared.Actions.Components;

namespace Content.Server.Internals
{
    [UsedImplicitly]
    public class InternalsSuitSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<InternalsSuitComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<InternalsSuitComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<InternalsSuitComponent, DroppedEvent>(OnDropped);
            SubscribeLocalEvent<InternalsSuitComponent, EntInsertedIntoContainerMessage>(OnContainerInserted);
            SubscribeLocalEvent<InternalsSuitComponent, EntRemovedFromContainerMessage>(OnContainerRemoved);
            SubscribeLocalEvent<InternalsSuitComponent, ToggleInternalsEvent>(OnToggleInternals);

            SubscribeLocalEvent<InternalsSuitComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<InternalsSuitComponent, InteractUsingEvent>(OnInteractUsing);
        }

        private ItemActionsComponent? GetActions(EntityUid uid)
        {
            ItemActionsComponent? actions = null;
            Resolve(uid, ref actions);
            return actions;
        }

        private void OnStartup(EntityUid uid, InternalsSuitComponent component, ComponentStartup args)
        {
            component.GasTankContainer = ContainerHelpers.EnsureContainer<ContainerSlot>(component.Owner, $"{component.Name}-gasTank");
        }

        private void OnShutdown(EntityUid uid, InternalsSuitComponent component, ComponentShutdown args)
            => SendToggleEvent(uid, component, new ToggleInternalsEvent(false, true, GetActions(uid)));

        public void OnDropped(EntityUid uid, InternalsSuitComponent component, DroppedEvent args)
            => SendToggleEvent(uid, component, new ToggleInternalsEvent(false, true, GetActions(uid)));

        public void OnContainerInserted(EntityUid uid, InternalsSuitComponent component, EntInsertedIntoContainerMessage args)
            => SendToggleEvent(uid, component, new ToggleInternalsEvent(false, true, GetActions(uid)));

        public void OnContainerRemoved(EntityUid uid, InternalsSuitComponent component, EntRemovedFromContainerMessage args)
            => SendToggleEvent(uid, component, new ToggleInternalsEvent(false, true, GetActions(uid)));

        public void OnToggleInternals(EntityUid uid, InternalsSuitComponent component, ToggleInternalsEvent args)
            => SendToggleEvent(uid, component, args);

        public void SendToggleEvent(EntityUid uid, InternalsSuitComponent component, ToggleInternalsEvent args)
        {
            if (component.GasTankPresent)
                RaiseLocalEvent(component.GasTankContainer.ContainedEntity!.Uid, args);
        }

        private void OnExamined(EntityUid uid, InternalsSuitComponent component, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange)
                return;

            // TODO: move to loc-string
            var color = component.GasTankPresent ? "green" : "red";
            var sign = component.GasTankPresent ? "." : "!";
            args.PushMarkup($"It is capable of holding an air tank which is currently [color={color}]{(component.GasTankPresent ? "attached" : "missing")}[/color]{sign}");

            // TODO: add a pressure gauge / approximate one?
            if (
                component.GasTankPresent
                && component.GasTankContainer.ContainedEntity!.TryGetComponent(out InternalsProviderComponent? provider)
            )
                Get<InternalsProviderSystem>().OnSuitExamined(uid, component, provider, args);
        }

        private async void OnInteractUsing(EntityUid uid, InternalsSuitComponent component, InteractUsingEvent args)
        {
            var user = args.User;

            if (!user.TryGetComponent(out SharedHandsComponent? hands))
            {
                component.Owner.PopupMessage(user, "No hands!"); // Do we even need to check that...? Why?
                return;
            }

            if (args.Used.HasComponent<InternalsProviderComponent>())
            {
                if (component.GasTankPresent)
                    component.Owner.PopupMessage(user, "Already have a tank inside!");
                else if (hands.TryPutHandIntoContainer(hands.ActiveHand!, component.GasTankContainer))
                    component.Owner.PopupMessage(user, $"Attached an air tank to {args.Target.Name}");
            }
            else if (await Get<ToolSystem>().UseTool(args.Used.Uid, args.User.Uid, component.Owner.Uid, 0f, 0.25f, "Screwing"))
            {
                if (!component.GasTankPresent)
                {
                    args.Target.PopupMessage(user, "There's no gas tank inside!");
                    return;
                }
                var entity = component.GasTankContainer.ContainedEntity!;
                if (component.GasTankContainer.Remove(entity!))
                    if (entity.TryGetComponent(out SharedItemComponent? item))
                        hands.PutInHand(item);
            }
            else
                component.Owner.PopupMessage(user, "Not a gas tank!");
        }
    }

    [RegisterComponent]
    public class InternalsSuitComponent : Component
    {
        public override string Name => "InternalsProxy";

        public ContainerSlot GasTankContainer = default!;

        public bool GasTankPresent => GasTankContainer.ContainedEntity is not null;
    }
}
