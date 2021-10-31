using Content.Server.Atmos.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Content.Shared.Hands.Components;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.IoC;
using Content.Shared.Interaction.Helpers;
using Content.Shared.ActionBlocker;
using Content.Server.Stack;
using Content.Server.Tools;
using Content.Server.Tools.Components;
using Content.Shared.Tools;
using Content.Shared.Tools.Components;
using System.Threading.Tasks;
using Content.Shared.Item;
using Robust.Shared.Map;

namespace Content.Server.Internals
{
    [UsedImplicitly]
    public class InternalsSuitSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<InternalsSuitComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<InternalsSuitComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<InternalsSuitComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<InternalsSuitComponent, ToggleInternalsEvent>(OnToggleInternals);
        }

        private void OnExamined(EntityUid uid, InternalsSuitComponent component, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange)
                return;

            // TODO: move to loc-string
            var color = component.GasTankPresent ? "green" : "red";
            var sign = component.GasTankPresent ? "." : "!";
            args.PushMarkup($"It is capable of holding an air tank which is currently [color={color}]{(component.GasTankPresent ? "attached" : "missing")}[/color]{sign}");
            if (
                component.GasTankPresent
                && component.GasTankContainer.ContainedEntity!.TryGetComponent(out InternalsProviderComponent? provider)
            )
                Get<InternalsProviderSystem>().OnSuitExamined(uid, component, provider, args);
        }

        private void OnStartup(EntityUid uid, InternalsSuitComponent component, ComponentStartup args)
        {
            component.GasTankContainer = ContainerHelpers.EnsureContainer<ContainerSlot>(component.Owner, $"{component.Name}-gasTank");
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

        public void OnToggleInternals(EntityUid uid, InternalsSuitComponent component, ToggleInternalsEvent args)
        {
            ToggleInternals(uid, component, args);
        }
        public bool ToggleInternals(EntityUid uid, InternalsSuitComponent component, ToggleInternalsEvent args)
        {
            return component.GasTankPresent
                && component.GasTankContainer.ContainedEntity!.TryGetComponent(out InternalsProviderComponent? provider)
                && Get<InternalsProviderSystem>().ToggleInternals(provider.Owner.Uid, provider!, args);
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
