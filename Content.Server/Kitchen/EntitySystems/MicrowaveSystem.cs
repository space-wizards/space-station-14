using Content.Server.Chemistry.EntitySystems;
using Content.Server.Kitchen.Components;
using Content.Server.Popups;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Robust.Shared.Player;
using JetBrains.Annotations;

namespace Content.Server.Kitchen.EntitySystems
{
    [UsedImplicitly]
    internal sealed class MicrowaveSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MicrowaveComponent, SolutionChangedEvent>(OnSolutionChange);
            SubscribeLocalEvent<MicrowaveComponent, InteractUsingEvent>(OnInteractUsing);
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

            if (!HasComp<SharedItemComponent>(args.Used))
            {
                _popupSystem.PopupEntity(Loc.GetString("microwave-component-interact-using-transfer-fail"), uid, Filter.Entities(args.User));
                return;
            }

            component.Storage.Insert(args.Used);
            component.DirtyUi();
        }
    }
}
