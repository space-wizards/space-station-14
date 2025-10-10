using System.Linq;
using Content.Shared.Hands;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Robust.Client.GameObjects;

namespace Content.Client.Items.Systems
{
    public sealed class EquippedVisualsSystem : EntitySystem
    {
        [Dependency] private readonly AppearanceSystem _appearance = default!;

        public override void Initialize()
        {
            base.Initialize();
            //SubscribeLocalEvent<ItemVisualizerComponent, GetInhandVisualsEvent>(OnGetInhandVisuals);
            SubscribeLocalEvent<ItemVisualizerComponent, GotEquippedEvent>(OnGotEquipped);

        }

        private void OnGotEquipped(Entity<ItemVisualizerComponent> ent, ref GotEquippedEvent args)
        {
            Log.Debug("EquippedVisualsSystem: OnGotEquipped");
        }

        /// <summary>
        ///     An entity holding this item is requesting visual information for in-hand sprites.
        /// </summary>
        private void OnGetInhandVisuals(Entity<ItemVisualizerComponent> ent, ref GetInhandVisualsEvent args)
        {
            var defaultKey = $"inhand-{args.Location.ToString().ToLowerInvariant()}";

            // try get explicit visuals
            if (!ent.Comp.InhandVisuals.TryGetValue(args.Location, out var layers))
            {
                // get defaults
                if (!TryGetDefaultVisuals(ent, defaultKey, out layers))
                    return;
            }

            AddSpriteLayers(defaultKey, layers, out var newLayers);

            args.Layers.AddRange(newLayers);
        }

        private bool TryGetDefaultVisuals(Entity<ItemVisualizerComponent> ent, string defaultKey, out List<PrototypeLayerData>? layers)
        {
            throw new NotImplementedException();
        }

        private void AddSpriteLayers(string defaultKey, List<PrototypeLayerData>? layers, out List<(string, PrototypeLayerData)> argLayers)
        {
            argLayers = new List<(string, PrototypeLayerData)>();

            if (layers == null)
                return;

            var i = 0;
            foreach (var layer in layers)
            {
                var key = layer.MapKeys?.FirstOrDefault();
                if (key == null)
                {
                    key = i == 0 ? defaultKey : $"{defaultKey}-{i}";
                    i++;
                }
                argLayers.Add((key, layer));
            }
        }

    }
}
