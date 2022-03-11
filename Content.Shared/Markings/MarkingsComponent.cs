using System.Collections.Generic;
using Content.Shared.CharacterAppearance;
using Robust.Shared.GameObjects;

namespace Content.Shared.Markings
{
    [RegisterComponent]
    public class MarkingsComponent : Component
    {
        public override string Name => "Markings";

        public string LastBase = "human";
        public Dictionary<HumanoidVisualLayers, List<Marking>> ActiveMarkings = new();
    }
}
