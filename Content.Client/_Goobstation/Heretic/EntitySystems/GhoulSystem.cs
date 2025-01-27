using Content.Shared.Examine;
using Content.Shared.Ghost.Roles.Components;
using Content.Shared.Heretic;
using Content.Shared.Humanoid;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Nutrition.AnimalHusbandry;
using Content.Shared.Nutrition.Components;
using Content.Shared.Zombies;
using Robust.Client.GameObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
