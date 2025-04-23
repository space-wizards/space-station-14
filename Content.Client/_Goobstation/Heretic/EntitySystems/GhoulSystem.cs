using Content.Shared.Heretic;
using Content.Shared.Humanoid;
using Robust.Client.GameObjects;
using System.Linq;

namespace Content.Client.Heretic.EntitySystems
{
    public sealed partial class GhoulSystem : Shared.Heretic.EntitySystems.SharedGhoulSystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GhoulComponent, ComponentStartup>(OnStartup);

        }


        public void OnStartup(EntityUid uid, GhoulComponent component, ComponentStartup args) {
            var ghoulcolor = Color.FromHex("#505050");

            if (!HasComp<HumanoidAppearanceComponent>(uid))
            {
                if (!TryComp<SpriteComponent>(uid, out var sprite))
                    return;

                for (var i = 0; i < sprite.AllLayers.Count(); i++)
                {
                    sprite.LayerSetColor(i, ghoulcolor);
                }
            }
        }
    }
}
