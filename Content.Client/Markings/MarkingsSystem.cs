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

        public override void Initialize()
        {
            SubscribeLocalEvent<MarkingsComponent, ComponentInit>(OnMarkingsInit);
            SubscribeLocalEvent<MarkingsComponent, SharedHumanoidAppearanceSystem.ChangedHumanoidAppearanceEvent>(UpdateMarkings);
        }

        private void OnMarkingsInit(EntityUid uid, MarkingsComponent component, ComponentInit __)
        {
        }

        public void ToggleMarkingVisibility(EntityUid uid, SpriteComponent body, HumanoidVisualLayers layer, bool toggle)
        {
            if(!EntityManager.TryGetComponent(uid, out MarkingsComponent? markings)) return;

            if (markings.ActiveMarkings.TryGetValue(layer, out List<Marking>? layerMarkings))
                foreach (Marking activeMarking in layerMarkings)
                    body.LayerSetVisible(activeMarking.MarkingId, toggle);
        }

        public void SetActiveMarkings(EntityUid uid, List<Marking> markingList, MarkingsComponent? markings = null)
        {
            if (!Resolve(uid, ref markings))
            {
                return;
            }

            markings.ActiveMarkings.Clear();

            foreach (HumanoidVisualLayers layer in HumanoidAppearanceSystem.BodyPartLayers)
            {
                markings.ActiveMarkings.Add(layer, new List<Marking>());
            }

            foreach (var marking in markingList)
            {
                markings.ActiveMarkings[_markingManager.Markings()[marking.MarkingId].BodyPart].Add(marking);
            }
        }
        
        public void UpdateMarkings(EntityUid uid, MarkingsComponent markings, SharedHumanoidAppearanceSystem.ChangedHumanoidAppearanceEvent args)
        {
            var appearance = args.Appearance;
            if (!EntityManager.TryGetComponent(uid, out SpriteComponent? sprite)) return;
            List<Marking> totalMarkings = new(appearance.Markings);

            Dictionary<MarkingCategories, MarkingPoints> usedPoints = new(markings.LayerPoints);

            foreach (var (category, points) in markings.LayerPoints)
            {
                usedPoints[category] = new MarkingPoints()
                {
                    Points = points.Points,
                    Required = points.Required,
                    DefaultMarkings = points.DefaultMarkings 
                };
            }

            // Reverse ordering
            for (int i = appearance.Markings.Count - 1; i >= 0; i--)
            {
                var marking = appearance.Markings[i];
                if (!_markingManager.IsValidMarking(marking, out MarkingPrototype? markingPrototype))
                {
                    continue;
                }

                if (usedPoints.TryGetValue(markingPrototype.MarkingCategory, out MarkingPoints? points))
                {
                    if (points.Points == 0)
                    {
                        continue;
                    }

                    points.Points--;
                }

                if (!sprite.LayerMapTryGet(markingPrototype.BodyPart, out int targetLayer))
                {
                    continue;
                }

                for (int j = 0; j < markingPrototype.Sprites.Count(); j++)
                {
                    var rsi = (SpriteSpecifier.Rsi) markingPrototype.Sprites[j];
                    string layerId = $"{markingPrototype.ID}-{rsi.RsiState}";

                    if (sprite.LayerMapTryGet(layerId, out var existingLayer))
                    {
                        sprite.RemoveLayer(existingLayer);
                        sprite.LayerMapRemove(marking.MarkingId);
                    }

                    int layer = sprite.AddLayer(markingPrototype.Sprites[j], targetLayer + j + 1);
                    sprite.LayerMapSet(layerId, layer);
                    if (markingPrototype.FollowSkinColor)
                    {
                        sprite.LayerSetColor(layerId, appearance.SkinColor);
                    }
                    else
                    {
                        sprite.LayerSetColor(layerId, marking.MarkingColors[j]);
                    }
                }

            }

            // for each layer, check if it's required and
            // if the points are greater than zero
            //
            // if so, then we start applying default markings
            // until the point requirement is satisfied - 
            // this can also mean that a specific set of markings
            // is applied on top of existing markings
            //
            // All default markings will follow the skin color of
            // the current body.
            foreach (var (layerType, points) in usedPoints)
            {
                if (points.Required && points.Points > 0)
                {
                    while (points.Points > 0)
                    {
                        // this all has to be checked, continues shouldn't occur because
                        // points.Points needs to be subtracted
                        if (points.DefaultMarkings.TryGetValue(points.Points - 1, out var marking)
                                && _markingManager.Markings().TryGetValue(marking, out var markingPrototype)
                                && markingPrototype.MarkingCategory == layerType // check if this actually belongs on this layer, too
                                && sprite.LayerMapTryGet(markingPrototype.BodyPart, out int targetLayer))
                        {
                            for (int j = 0; j < markingPrototype.Sprites.Count(); j++)
                            {
                                var rsi = (SpriteSpecifier.Rsi) markingPrototype.Sprites[j];
                                string layerId = $"{markingPrototype.ID}-{rsi.RsiState}";

                                if (sprite.LayerMapTryGet(layerId, out var existingLayer))
                                {
                                    sprite.RemoveLayer(existingLayer);
                                    sprite.LayerMapRemove(markingPrototype.ID);
                                }

                                int layer = sprite.AddLayer(markingPrototype.Sprites[j], targetLayer + j + 1);
                                sprite.LayerMapSet(layerId, layer);
                                sprite.LayerSetColor(layerId, appearance.SkinColor);
                            }

                            totalMarkings.Add(markingPrototype.AsMarking());
                        }

                        points.Points--;
                    }
                }
            }

            SetActiveMarkings(uid, totalMarkings, markings);
        }
    }
}
