using Content.Client.Items;
using Content.Client.Storage.Systems;
using Content.Shared.Stacks;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Stack
{
    /// <inheritdoc />
    [UsedImplicitly]
    public sealed class StackSystem : SharedStackSystem
    {
        [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
        [Dependency] private readonly ItemCounterSystem _counterSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<StackComponent, AppearanceChangeEvent>(OnAppearanceChange);
            Subs.ItemStatus<StackComponent>(ent => new StackStatusControl(ent));
        }

        #region Appearance

        private void OnAppearanceChange(Entity<StackComponent> ent, ref AppearanceChangeEvent args)
        {
            var (uid, comp) = ent;

            if (args.Sprite == null || comp.LayerStates.Count < 1)
                return;

            // Skip processing if no elements in the stack
            if (!_appearanceSystem.TryGetData<int>(uid, StackVisuals.Actual, out var actual, args.Component))
                return;

            if (!_appearanceSystem.TryGetData<int>(uid, StackVisuals.MaxCount, out var maxCount, args.Component))
                maxCount = comp.LayerStates.Count;

            if (!_appearanceSystem.TryGetData<bool>(uid, StackVisuals.Hide, out var hidden, args.Component))
                hidden = false;

            if (comp.LayerFunction != StackLayerFunction.None)
                ApplyLayerFunction((uid, comp), ref actual, ref maxCount);

            if (comp.IsComposite)
            {
                _counterSystem.ProcessCompositeSprite(uid,
                                                    actual,
                                                    maxCount,
                                                    comp.LayerStates,
                                                    hidden,
                                                    sprite: args.Sprite);
            }
            else
            {
                _counterSystem.ProcessOpaqueSprite(uid,
                                                comp.BaseLayer,
                                                actual,
                                                maxCount,
                                                comp.LayerStates,
                                                hidden,
                                                sprite: args.Sprite);
            }
        }

        /// <summary>
        /// Adjusts the actual and maxCount to change how stack amounts are displayed.
        /// </summary>
        /// <param name="ent">The entity considered.</param>
        /// <param name="actual">The actual number of items in the stack. Altered depending on the function to run.</param>
        /// <param name="maxCount">The maximum number of items in the stack. Altered depending on the function to run.</param>
        /// <returns>True if a function was applied.</returns>
        private bool ApplyLayerFunction(Entity<StackComponent> ent, ref int actual, ref int maxCount)
        {
            switch (ent.Comp.LayerFunction)
            {
                case StackLayerFunction.Threshold:
                    if (TryComp<StackLayerThresholdComponent>(ent, out var threshold))
                    {
                        ApplyThreshold(threshold, ref actual, ref maxCount);
                        return true;
                    }

                    break;
            }

            // No function applied.
            return false;
        }

        /// <summary>
        /// Selects which layer a stack applies based on a list of thresholds.
        /// Each threshold passed results in the next layer being selected.
        /// </summary>
        /// <param name="comp">The threshold parameters to apply.</param>
        /// <param name="actual">The number of items in the stack. Will be set to the index of the layer to use.</param>
        /// <param name="maxCount">The maximum possible number of items in the stack. Will be set to the number of selectable layers.</param>
        private static void ApplyThreshold(StackLayerThresholdComponent comp, ref int actual, ref int maxCount)
        {
            // We must stop before we run out of thresholds or layers, whichever's smaller.
            maxCount = Math.Min(comp.Thresholds.Count + 1, maxCount);
            var newActual = 0;
            foreach (var threshold in comp.Thresholds)
            {
                //If our value exceeds threshold, the next layer should be displayed.
                //Note: we must ensure actual <= MaxCount.
                if (actual >= threshold && newActual < maxCount)
                    newActual++;
                else
                    break;
            }

            actual = newActual;
        }

        #endregion
    }
}
