using System.Collections.Generic;
using Content.Shared.CharacterAppearance;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Markings
{
    [RegisterComponent]
    public sealed class MarkingsComponent : Component
    {
        public override string Name => "Markings";

        public string LastBase = "human";
        public Dictionary<HumanoidVisualLayers, List<Marking>> ActiveMarkings = new();

        // Layer points for the attached mob. This is verified client side (FOR NOW),
        // but upon render for the given entity with this component, it will start subtracting
        // points from this set. Upon depletion, no more sprites in this layer will be
        // rendered. If an entry is null, however, it is considered 'unlimited points' for
        // that layer.
        //
        // Layer points are useful for restricting the amount of markings a specific layer can use
        // for specific mobs (i.e., a lizard should only use one set of horns and maybe two frills),
        // and all species with selectable tails should have exactly one tail)
        //
        // If something is required, then something must be selected in that category. Otherwise,
        // the first instance of a marking in that category will be added to a character
        // upon round start.
        [DataField("layerPoints")]
        public Dictionary<HumanoidVisualLayers, MarkingPoints> LayerPoints = new();
    }

    public sealed class ActiveMarking
    {
        public Marking Marking { get; }
        public bool IsDefault { get; }

        public ActiveMarking(Marking marking, bool isDefault)
        {
            Marking = marking;
            IsDefault = isDefault;
        }
    }

    [DataDefinition]
    public sealed class MarkingPoints
    {
        [DataField("points", required: true)]
        public int Points = 0;
        [DataField("required", required: true)]
        public bool Required = false;
        // Default markings for this layer.
        [DataField("defaultMarkings")]
        public List<string> DefaultMarkings = new();
    }
}
