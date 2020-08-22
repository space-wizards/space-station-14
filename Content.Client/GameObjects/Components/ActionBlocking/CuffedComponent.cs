
using Content.Client.Utility;
using Robust.Client.Graphics;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Shared.IoC;
using Robust.Shared.GameObjects;
using Content.Shared.GameObjects.Components.ActionBlocking;
using Content.Shared.Preferences.Appearance;
using Robust.Client.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components.ActionBlocking
{
    [RegisterComponent]
    public class CuffedComponent : SharedCuffedComponent
    {
#pragma warning disable 0649
        [Dependency] private readonly IResourceCache _resourceCache;
#pragma warning restore 0649

        [ViewVariables]
        private string _texturePath = default;

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            var cuffState = curState as CuffedComponentState;

            if (cuffState == null)
            {
                return;
            }

            CanStillInteract = cuffState.CanStillInteract;

            if (Owner.TryGetComponent<SpriteComponent>(out var sprite))
            {
                if (_texturePath != cuffState.TexturePath) // avoid doing this bit of logic when the texture remains the same
                {
                    _texturePath = cuffState.TexturePath;
                    Texture tex = _resourceCache.GetTexture(_texturePath);
                    sprite.LayerSetTexture(HumanoidVisualLayers.Handcuffs, tex);
                }

                sprite.LayerSetVisible(HumanoidVisualLayers.Handcuffs, !CanStillInteract);
            }
        }

        public override void OnRemove()
        {
            base.OnRemove();

            if (Owner.TryGetComponent<SpriteComponent>(out var sprite))
            {
                sprite.LayerSetVisible(HumanoidVisualLayers.Handcuffs, false);
            }
        }
    }
}
