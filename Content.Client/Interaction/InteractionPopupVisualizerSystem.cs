using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Shared.Interaction;
using Robust.Client.GameObjects;

namespace Content.Client.Interaction
{
    public class InteractionPopupVisualizerSystem : VisualizerSystem<InteractionPopupComponent>
    {
        protected override void OnAppearanceChange(EntityUid uid, InteractionPopupComponent component, ref AppearanceChangeEvent args)
            if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        sprite.ChangeSprite(state);

    }
}
