using System.Collections.Generic;
using Content.Shared.GameTicking;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;
using static Content.Shared.GameObjects.Components.SharedWiresComponent;

namespace Content.Server.GameObjects.EntitySystems
{
    public class WireHackingSystem : EntitySystem, IResettingEntitySystem
    {
        [ViewVariables] private readonly Dictionary<string, WireLayout> _layouts =
            new();

        public bool TryGetLayout(string id, out WireLayout layout)
        {
            return _layouts.TryGetValue(id, out layout);
        }

        public void AddLayout(string id, WireLayout layout)
        {
            _layouts.Add(id, layout);
        }

        public void Reset()
        {
            _layouts.Clear();
        }
    }

    public sealed class WireLayout
    {
        [ViewVariables] public IReadOnlyDictionary<object, WireData> Specifications { get; }

        public WireLayout(IReadOnlyDictionary<object, WireData> specifications)
        {
            Specifications = specifications;
        }

        public sealed class WireData
        {
            public WireLetter Letter { get; }
            public WireColor Color { get; }
            public int Position { get; }

            public WireData(WireLetter letter, WireColor color, int position)
            {
                Letter = letter;
                Color = color;
                Position = position;
            }
        }
    }
}
