
using System.Collections.Generic;
using Content.Shared.Rounding;
using Content.Shared.Stacks;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

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
    /// - type: Appearance
    ///   visuals:
    ///     - type: StackVisualizer
    ///       stackLayers:
    ///         - goldbar_10
    ///         - goldbar_20
    ///         - goldbar_30
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
    [UsedImplicitly]
    public sealed class StackVisualizer : AppearanceVisualizer
    {
        /// <summary>
        /// Default IconLayer stack.
        /// </summary>
        private const int IconLayer = 0;

        /// <summary>
        /// Sprite layers used in stack visualizer. Sprites first in layer correspond to lower stack states
        /// e.g. <code>_spriteLayers[0]</code> is lower stack level than <code>_spriteLayers[1]</code>.
        /// </summary>
        [DataField("stackLayers")] private readonly List<string> _spriteLayers = new();

        /// <summary>
        /// Determines if the visualizer uses composite or non-composite layers for icons. Defaults to false.
        ///
        /// <list type="bullet">
        /// <item>
        /// <description>false: they are opaque and mutually exclusive (e.g. sprites in a cable coil). <b>Default value</b></description>
        /// </item>
        /// <item>
        /// <description>true: they are transparent and thus layered one over another in ascending order first</description>
        /// </item>
        /// </list>
        ///
        /// </summary>
        [DataField("composite")] private bool _isComposite;

        [DataField("sprite")] private ResourcePath? _spritePath;

        [Obsolete("Subscribe to your component being initialised instead.")]
        public override void InitializeEntity(EntityUid entity)
        {
            base.InitializeEntity(entity);

            if (_isComposite
                && _spriteLayers.Count > 0
                && IoCManager.Resolve<IEntityManager>().TryGetComponent<SpriteComponent?>(entity, out var spriteComponent))
            {
                var spritePath = _spritePath ?? spriteComponent.BaseRSI!.Path!;

                foreach (var sprite in _spriteLayers)
                {
                    spriteComponent.LayerMapReserveBlank(sprite);
                    spriteComponent.LayerSetSprite(sprite, new SpriteSpecifier.Rsi(spritePath, sprite));
                    spriteComponent.LayerSetVisible(sprite, false);
                }
            }
        }

        [Obsolete("Subscribe to AppearanceChangeEvent instead.")]
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var entities = IoCManager.Resolve<IEntityManager>();
            if (entities.TryGetComponent(component.Owner, out SpriteComponent? spriteComponent))
            {
                if (_isComposite)
                {
                    ProcessCompositeSprites(component, spriteComponent);
                }
                else
                {
                    ProcessOpaqueSprites(component, spriteComponent);
                }
            }
        }

        private void ProcessOpaqueSprites(AppearanceComponent component, SpriteComponent spriteComponent)
        {
            // Skip processing if no actual
            if (!component.TryGetData<int>(StackVisuals.Actual, out var actual)) return;
            if (!component.TryGetData<int>(StackVisuals.MaxCount, out var maxCount))
            {
                maxCount = _spriteLayers.Count;
            }

            var activeLayer = ContentHelpers.RoundToEqualLevels(actual, maxCount, _spriteLayers.Count);
            spriteComponent.LayerSetState(IconLayer, _spriteLayers[activeLayer]);
        }

        private void ProcessCompositeSprites(AppearanceComponent component, SpriteComponent spriteComponent)
        {
            // If hidden, don't render any sprites
            if (component.TryGetData<bool>(StackVisuals.Hide, out var hide)
                && hide)
            {
                foreach (var transparentSprite in _spriteLayers)
                {
                    spriteComponent.LayerSetVisible(transparentSprite, false);
                }

                return;
            }

            // Skip processing if no actual/maxCount
            if (!component.TryGetData<int>(StackVisuals.Actual, out var actual)) return;
            if (!component.TryGetData<int>(StackVisuals.MaxCount, out var maxCount))
            {
                maxCount = _spriteLayers.Count;
            }


            var activeTill = ContentHelpers.RoundToNearestLevels(actual, maxCount, _spriteLayers.Count);
            for (var i = 0; i < _spriteLayers.Count; i++)
            {
                spriteComponent.LayerSetVisible(_spriteLayers[i], i < activeTill);
            }
        }
    }
}
