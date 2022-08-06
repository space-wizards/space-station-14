using Content.Server.Chemistry.EntitySystems;
using Content.Server.Construction;
using Content.Server.Kitchen.Components;
using Content.Server.Popups;
using Content.Shared.Destructible;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Kitchen.Components;
using Robust.Shared.Player;
using JetBrains.Annotations;
using Content.Shared.Interaction.Events;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Popups;
using Content.Server.Hands.Systems;

namespace Content.Server.Kitchen.EntitySystems
{
    [UsedImplicitly]
    internal sealed class MicrowaveSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly HandsSystem _handsSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MicrowaveComponent, SolutionChangedEvent>(OnSolutionChange);
            SubscribeLocalEvent<MicrowaveComponent, InteractUsingEvent>(OnInteractUsing, after: new[]{typeof(AnchorableSystem)});
            SubscribeLocalEvent<MicrowaveComponent, BreakageEventArgs>(OnBreak);
            SubscribeLocalEvent<MicrowaveComponent, SuicideEvent>(OnSuicide);
        }

        private void OnSuicide(EntityUid uid, MicrowaveComponent component, SuicideEvent args)
        {
            if (args.Handled) return;
            args.SetHandled(SuicideKind.Heat);
            var victim = args.Victim;
            var headCount = 0;

            if (TryComp<SharedBodyComponent?>(victim, out var body))
            {
                var headSlots = body.GetSlotsOfType(BodyPartType.Head);

                foreach (var slot in headSlots)
                {
                    var part = slot.Part;

                    if (part == null ||
                        !body.TryDropPart(slot, out var dropped))
                    {
                        continue;
                    }

                    foreach (var droppedPart in dropped.Values)
                    {
                        if (droppedPart.PartType != BodyPartType.Head)
                        {
                            continue;
                        }
                        component.Storage.Insert(droppedPart.Owner);
                        headCount++;
                    }
                }
            }

            var othersMessage = headCount > 1
                ? Loc.GetString("microwave-component-suicide-multi-head-others-message", ("victim", victim))
                : Loc.GetString("microwave-component-suicide-others-message", ("victim", victim));

            victim.PopupMessageOtherClients(othersMessage);

            var selfMessage = headCount > 1
                ? Loc.GetString("microwave-component-suicide-multi-head-message")
                : Loc.GetString("microwave-component-suicide-message");

            victim.PopupMessage(selfMessage);
            component.ClickSound();
            component.SetCookTime(10);
            component.Wzhzhzh();
        }

        private void OnSolutionChange(EntityUid uid, MicrowaveComponent component, SolutionChangedEvent args)
        {
            component.DirtyUi();
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var comp in EntityManager.EntityQuery<MicrowaveComponent>())
            {
                comp.OnUpdate();
            }
        }

        private void OnInteractUsing(EntityUid uid, MicrowaveComponent component, InteractUsingEvent args)
        {
            if(args.Handled) return;
            if (!component.Powered)
            {
                _popupSystem.PopupEntity(Loc.GetString("microwave-component-interact-using-no-power"), uid, Filter.Entities(args.User));
                return;
            }

            if (component.Broken)
            {
                _popupSystem.PopupEntity(Loc.GetString("microwave-component-interact-using-broken"), uid, Filter.Entities(args.User));
                return;
            }

            if (!HasComp<ItemComponent>(args.Used))
            {
                _popupSystem.PopupEntity(Loc.GetString("microwave-component-interact-using-transfer-fail"), uid, Filter.Entities(args.User));
                return;
            }

            args.Handled = true;
            _handsSystem.TryDropIntoContainer(args.User, args.Used, component.Storage);
            component.DirtyUi();
        }

        private void OnBreak(EntityUid uid, MicrowaveComponent component, BreakageEventArgs args)
        {
            component.Broken = true;
            component.SetAppearance(MicrowaveVisualState.Broken);
        }
    }
}
