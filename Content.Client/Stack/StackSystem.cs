using Content.Client.Items;
using Content.Client.Storage.Systems;
using Content.Shared.Stacks;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Stack
{
    [UsedImplicitly]
    public sealed class StackSystem : SharedStackSystem
    {
        [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
        [Dependency] private readonly ItemCounterSystem _counterSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<StackComponent, ItemStatusCollectMessage>(OnItemStatus);
            SubscribeLocalEvent<StackComponent, AppearanceChangeEvent>(OnAppearanceChange);
        }

        private void OnItemStatus(EntityUid uid, StackComponent component, ItemStatusCollectMessage args)
        {
            args.Controls.Add(new StackStatusControl(component));
        }

        public override void SetCount(EntityUid uid, int amount, StackComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            base.SetCount(uid, amount, component);

            // TODO PREDICT ENTITY DELETION: This should really just be a normal entity deletion call.
            if (component.Count <= 0)
            {
                Xform.DetachParentToNull(uid, Transform(uid));
                return;
            }

            // Dirty the UI now that the stack count has changed.
            if (component is StackComponent clientComp)
                clientComp.UiUpdateNeeded = true;
        }

        private void OnAppearanceChange(EntityUid uid, StackComponent comp, ref AppearanceChangeEvent args)
        {
            if (args.Sprite == null || comp.LayerStates.Count < 1)
                return;

            // Skip processing if no actual
            if (!_appearanceSystem.TryGetData<int>(uid, StackVisuals.Actual, out var actual, args.Component))
                return;

            if (!_appearanceSystem.TryGetData<int>(uid, StackVisuals.MaxCount, out var maxCount, args.Component))
                maxCount = comp.LayerStates.Count;

            if (!_appearanceSystem.TryGetData<bool>(uid, StackVisuals.Hide, out var hidden, args.Component))
                hidden = false;

            if (comp.IsComposite)
                _counterSystem.ProcessCompositeSprite(uid, actual, maxCount, comp.LayerStates, hidden, sprite: args.Sprite);
            else
                _counterSystem.ProcessOpaqueSprite(uid, comp.BaseLayer, actual, maxCount, comp.LayerStates, hidden, sprite: args.Sprite);
        }
    }
}
