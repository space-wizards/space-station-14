using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Shared.GameObjects.Components.Nutrition;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Nutrition
{
    /// <summary>
    /// This container acts as a master object for things like Pizza, which holds slices.
    /// </summary>
    /// TODO: Perhaps implement putting food back (pizza boxes) but that really isn't mandatory.
    /// This doesn't even need to have an actual Container for right now.
    [RegisterComponent]
    public sealed class FoodContainer : SharedFoodContainerComponent, IUse
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        public override string Name => "FoodContainer";

        private AppearanceComponent? _appearance;
        [DataField("prototypes")]
        private Dictionary<string, int>? _prototypes = default;
        [DataField("capacity")]
        private uint _capacity = 5;

        public int Capacity => (int)_capacity;
        [ViewVariables]
        public int Count => _count;

        private int _count = 0;

        public override void Initialize()
        {
            base.Initialize();
            Owner.TryGetComponent(out _appearance);
            _count = Capacity;
            UpdateAppearance();

        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out HandsComponent? handsComponent))
            {
                return false;
            }

            var itemToSpawn = Owner.EntityManager.SpawnEntity(GetRandomPrototype(), Owner.Transform.Coordinates);
            handsComponent.PutInHandOrDrop(itemToSpawn.GetComponent<ItemComponent>());
            _count--;
            if (_count < 1)
            {
                Owner.Delete();
                return false;
            }

            return true;
        }

        private string? GetRandomPrototype()
        {
            var defaultProto = _prototypes?.Keys.FirstOrDefault();

            if (defaultProto == null)
            {
                return null;
            }

            DebugTools.AssertNotNull(_prototypes);

            if (_prototypes!.Count == 1)
            {
                return defaultProto;
            }

            var probResult = _random.Next(0, 100);
            var total = 0;
            foreach (var item in _prototypes)
            {
                total += item.Value;
                if (probResult < total)
                {
                    return item.Key;
                }
            }

            return defaultProto;
        }

        private void UpdateAppearance()
        {
            _appearance?.SetData(FoodContainerVisuals.Current, Count);
        }
    }
}
