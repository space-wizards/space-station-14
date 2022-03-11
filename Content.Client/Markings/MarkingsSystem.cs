using System.Collections.Generic;
using System.Linq;
using Content.Client.CharacterAppearance.Systems;
using Content.Shared.Markings;
using Content.Shared.CharacterAppearance;
using Content.Shared.CharacterAppearance.Systems;
using Content.Shared.Preferences;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Content.Client.Markings
{
    public sealed class MarkingsSystem : EntitySystem
    {
        [Dependency] private readonly MarkingManager _markingManager = default!;
        // [Dependency] private readonly MarkingsSpeciesManager _speciesManager = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<MarkingsComponent, ComponentInit>(OnMarkingsInit);
            SubscribeLocalEvent<MarkingsComponent, SharedHumanoidAppearanceSystem.ChangedHumanoidAppearanceEvent>(UpdateMarkings);
        }

        private void OnMarkingsInit(EntityUid uid, MarkingsComponent component, ComponentInit __)
        {
            foreach (HumanoidVisualLayers layer in HumanoidAppearanceSystem.BodyPartLayers)
            {
                component.ActiveMarkings.Add(layer, new List<Marking>());
            }

        }

        public void ToggleMarkingVisibility(EntityUid uid, SpriteComponent body, HumanoidVisualLayers layer, bool toggle)
        {
            if(!EntityManager.TryGetComponent(uid, out MarkingsComponent? markings)) return;

            if (markings.ActiveMarkings.TryGetValue(layer, out List<Marking>? layerMarkings))
                foreach (Marking marking in layerMarkings)
                    body.LayerSetVisible(marking.MarkingId, toggle);
        }
        
        public void UpdateMarkings(EntityUid uid, MarkingsComponent markings, SharedHumanoidAppearanceSystem.ChangedHumanoidAppearanceEvent args)
        {
            var appearance = args.Appearance;
            if (!EntityManager.TryGetComponent(uid, out SpriteComponent? sprite)) return;

            // Top -> Bottom ordering
            for (int i = appearance.Markings.Count; i > 0; i--)
            {
                var marking = appearance.Markings[i];
                if (!_markingManager.IsValidMarking(marking, out MarkingPrototype? markingPrototype))
                {
                    continue;
                }

                if (!sprite.LayerMapTryGet(markingPrototype.BodyPart, out int targetLayer))
                {
                    continue;
                }

                for (int j = 0; i < markingPrototype.Sprites.Count(); i++)
                {
                    string layerId = markingPrototype.ID + markingPrototype.MarkingPartNames[j];

                    if (sprite.LayerMapTryGet(layerId, out var existingLayer))
                    {
                        sprite.RemoveLayer(existingLayer);
                        sprite.LayerMapRemove(marking.MarkingId);
                    }

                    int layer = sprite.AddLayer(markingPrototype.Sprites[j], targetLayer + j + 1);
                    sprite.LayerMapSet(layerId, layer);
                    sprite.LayerSetColor(layerId, marking.MarkingColors[j]);
                }

                // _activeMarkings[markingPrototype.BodyPart].Add(marking);
            }
        }
    }
}
