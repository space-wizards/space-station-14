namespace Content.Shared.Humanoid.Markings
{
    [RegisterComponent]
    public sealed partial class MarkingsComponent : Component
    {
        public Dictionary<HumanoidVisualLayers, List<Marking>> ActiveMarkings = new();

        // Layer points for the attached mob. This is verified client side (but should be verified server side, eventually as well),
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
        public Dictionary<MarkingCategories, MarkingPoints> LayerPoints = new();
    }


}
