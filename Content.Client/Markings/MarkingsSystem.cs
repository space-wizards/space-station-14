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
            SubscribeLocalEvent<MarkingsComponent, SharedHumanoidAppearanceSystem.ChangedHumanoidAppearanceEvent>(UpdateMarkings);
        }

        public void ToggleMarkingVisibility(EntityUid uid, SpriteComponent body, HumanoidVisualLayers layer, bool toggle)
        {
            if(!EntityManager.TryGetComponent(uid, out MarkingsComponent? markings)) return;

            if (markings.ActiveMarkings.TryGetValue(layer, out List<Marking>? layerMarkings))
                foreach (Marking activeMarking in layerMarkings)
                    body.LayerSetVisible(activeMarking.MarkingId, toggle);
        }

        public void SetActiveMarkings(EntityUid uid, MarkingsSet markingList, MarkingsComponent? markings = null)
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
                if (_markingManager.Markings().TryGetValue(marking.MarkingId, out var markingProto))
                {
                    markings.ActiveMarkings[markingProto.BodyPart].Add(marking);
                }
            }
        }

        public void UpdateMarkings(EntityUid uid, MarkingsComponent markings, SharedHumanoidAppearanceSystem.ChangedHumanoidAppearanceEvent args)
        {
            var appearance = args.Appearance;
            if (!EntityManager.TryGetComponent(uid, out SpriteComponent? sprite)) return;
            MarkingsSet totalMarkings = new MarkingsSet(appearance.Markings);

            Dictionary<MarkingCategories, MarkingPoints> usedPoints = MarkingPoints.CloneMarkingPointDictionary(markings.LayerPoints);

            var markingsEnumerator = appearance.Markings.GetReverseEnumerator();
            // Reverse ordering
            while (markingsEnumerator.MoveNext())
            {
                var marking = (Marking) markingsEnumerator.Current;
                if (!_markingManager.IsValidMarking(marking, out MarkingPrototype? markingPrototype))
                {
                    continue;
                }

                // if the given marking isn't correctly formed, we need to
                // instead just allocate a new marking based on the old one

                if (marking.MarkingColors.Count != markingPrototype.Sprites.Count)
                {
                    marking = new Marking(marking.MarkingId, markingPrototype.Sprites.Count);
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

                            totalMarkings.AddBack(markingPrototype.AsMarking());
                        }

                        points.Points--;
                    }
                }
            }

            SetActiveMarkings(uid, totalMarkings, markings);
        }
    }
}
