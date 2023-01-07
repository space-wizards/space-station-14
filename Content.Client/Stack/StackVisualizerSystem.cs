using Content.Shared.Rounding;
using Content.Shared.Stacks;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;

namespace Content.Client.Stack
{
    /// <summary>
    /// Visualizer for items that come in stacks and have different appearance
    /// depending on the size of the stack. Visualizer can work by switching between different
    /// icons in <c>_spriteLayers</c> or if the sprite layers are supposed to be composed as transparent layers.
    /// The former behavior is default and the latter behavior can be defined in prototypes.
    ///
    /// <example>
    /// <para>To define a Stack Visualizer prototype insert the following
    /// snippet (you can skip Appearance if already defined)
    /// </para>
    /// <code>
    /// - type: StackVisualizer
    ///   stackLayers:
    ///     - goldbar_10
    ///     - goldbar_20
    ///     - goldbar_30
    /// </code>
    /// </example>
    /// <example>
    /// <para>Defining a stack visualizer with composable transparent layers</para>
    /// <code>
    ///   - type: StackVisualizer
    ///     composite: true
    ///     stackLayers:
    ///       - cigarette_1
    ///       - cigarette_2
    ///       - cigarette_3
    ///       - cigarette_4
    ///       - cigarette_5
    ///       - cigarette_6
    /// </code>
    /// </example>
    ///  <seealso cref="_spriteLayers"/>
    /// </summary>
    public sealed class StackVisualizerSystem : VisualizerSystem<StackVisualizerComponent>
    {
        /// <summary>
        /// Default IconLayer stack.
        /// </summary>
        private const int IconLayer = 0;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<StackVisualizerComponent, ComponentInit>(OnComponentInit);
        }

        private void OnComponentInit(EntityUid uid, StackVisualizerComponent stackComponent, ComponentInit args)
        {
            if (RequiresCompositeInit(uid, stackComponent, out var spriteComponent))
            {
                var spritePath = stackComponent.SpritePath ?? spriteComponent.BaseRSI!.Path!;

                foreach (var sprite in stackComponent.SpriteLayers)
                {
                    spriteComponent.LayerMapReserveBlank(sprite);
                    spriteComponent.LayerSetSprite(sprite, new SpriteSpecifier.Rsi(spritePath, sprite));
                    spriteComponent.LayerSetVisible(sprite, false);
                }
            }
        }

        protected override void OnAppearanceChange(EntityUid uid, StackVisualizerComponent stackComponent, ref AppearanceChangeEvent args)
        {
            if (args.Sprite == null)
            {
                return;
            }

            if (stackComponent.IsComposite)
            {
                ProcessCompositeSprites(uid, stackComponent, args.Sprite);
            }
            else
            {
                ProcessOpaqueSprites(uid, stackComponent, args.Sprite);
            }
        }

        private void ProcessOpaqueSprites(EntityUid uid, StackVisualizerComponent stackComponent, ISpriteComponent spriteComponent)
        {
            // Skip processing if no actual
            if (!AppearanceSystem.TryGetData<int>(uid, StackVisuals.Actual, out var actual))
            {
                return;
            }

            if (!AppearanceSystem.TryGetData<int>(uid, StackVisuals.MaxCount, out var maxCount))
            {
                maxCount = stackComponent.SpriteLayers.Count;
            }

            var activeLayer = ContentHelpers.RoundToEqualLevels(actual, maxCount, stackComponent.SpriteLayers.Count);
            spriteComponent.LayerSetState(IconLayer, stackComponent.SpriteLayers[activeLayer]);
        }

        // Returns `true` and sets `spriteOut` if the `stackComponent` requires the
        // sprite to have additional layers configured for the stack visualizer to work.
        private bool RequiresCompositeInit(EntityUid uid, StackVisualizerComponent stackComponent, [NotNullWhen(true)] out ISpriteComponent? spriteOut)
        {
            spriteOut = null;
            if ( !stackComponent.IsComposite || stackComponent.SpriteLayers.Count == 0)
            {
                return false;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();
            return entityManager.TryGetComponent<ISpriteComponent?>(uid, out spriteOut);
        }


        private void ProcessCompositeSprites(EntityUid uid, StackVisualizerComponent stackComponent, ISpriteComponent spriteComponent)
        {
            // If hidden, don't render any sprites
            if (AppearanceSystem.TryGetData<bool>(uid, StackVisuals.Hide, out var hide)
                && hide)
            {
                foreach (var transparentSprite in stackComponent.SpriteLayers)
                {
                    spriteComponent.LayerSetVisible(transparentSprite, false);
                }

                return;
            }

            // Skip processing if no actual/maxCount
            if (!AppearanceSystem.TryGetData<int>(uid, StackVisuals.Actual, out var actual)) return;
            if (!AppearanceSystem.TryGetData<int>(uid, StackVisuals.MaxCount, out var maxCount))
            {
                maxCount = stackComponent.SpriteLayers.Count;
            }


            var activeTill = ContentHelpers.RoundToNearestLevels(actual, maxCount, stackComponent.SpriteLayers.Count);
            for (var i = 0; i < stackComponent.SpriteLayers.Count; i++)
            {
                spriteComponent.LayerSetVisible(stackComponent.SpriteLayers[i], i < activeTill);
            }
        }

    }
}
