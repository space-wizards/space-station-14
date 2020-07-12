using System.Collections.Generic;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.ViewVariables;
using static Content.Shared.GameObjects.Components.SharedWiresComponent;

namespace Content.Server.Interfaces.GameObjects.Components.Interaction
{
    public class WireHackingSystem : EntitySystem
    {
        [ViewVariables] private readonly Dictionary<string, WireLayout> _layouts =
            new Dictionary<string, WireLayout>();

        public bool TryGetLayout(string id, out WireLayout layout)
        {
            return _layouts.TryGetValue(id, out layout);
        }

        public void AddLayout(string id, WireLayout layout)
        {
            _layouts.Add(id, layout);
        }

        public void ResetLayouts()
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
