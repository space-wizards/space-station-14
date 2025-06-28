using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Humanoid.Markings
{
    [Prototype] // Floof
    public sealed partial class MarkingPrototype : IPrototype
    {
        [IdDataField]
        public string ID { get; private set; } = "uwu";

        public string Name { get; private set; } = default!;

        [DataField("bodyPart", required: true)]
        public HumanoidVisualLayers BodyPart { get; private set; } = default!;

        [DataField("markingCategory", required: true)]
        public MarkingCategories MarkingCategory { get; private set; } = default!;

        [DataField("speciesRestriction")]
        public List<string>? SpeciesRestrictions { get; private set; }

        [DataField("sexRestriction")]
        public Sex? SexRestriction { get; private set; }

        [DataField("followSkinColor")]
        public bool FollowSkinColor { get; private set; } = false;

        [DataField("forcedColoring")]
        public bool ForcedColoring { get; private set; } = false;

        [DataField("coloring")]
        public MarkingColors Coloring { get; private set; } = new();

        /// <summary>
        /// Do we need to apply any displacement maps to this marking? Set to false if your marking is incompatible
        /// with a standard human doll, and is used for some special races with unusual shapes
        /// </summary>
        [DataField]
        public bool CanBeDisplaced { get; private set; } = true;

        [DataField("sprites", required: true)]
        public List<SpriteSpecifier> Sprites { get; private set; } = default!;

        [DataField]
        public string? Shader { get; private set; } = null; // imp

        /// <summary>
        /// Allows specific images to be put into any arbitrary layer on the mob.
        /// Whole point of this is to have things like tails be able to be
        /// behind the mob when facing south-east-west, but in front of the mob
        /// when facing north. This requires two+ sprites, each in a different
        /// layer.
        /// Is a dictionary: sprite name -> layer name,
        /// e.g. "tail-cute-vulp" -> "tail-back", "tail-cute-vulp-oversuit" -> "tail-oversuit"
        /// also, FLOOF ADD =3
        /// </summary>
        [DataField]
        public Dictionary<string, string>? Layering { get; private set; }

        /// <summary>
        /// Allows you to link a specific sprite's coloring to another sprite's coloring.
        /// This is useful for things like tails, which while they have two sets of sprites,
        /// the two sets of sprites should be treated as one sprite for the purposes of
        /// coloring. Just more intuitive that way~
        /// Format: spritename getting colored -> spritename which colors it
        /// so if we have a Tail Behind with 'cooltail' as the sprite name, and a Tail Oversuit
        /// with 'cooltail-oversuit' as the sprite name, and we want to have the Tail Behind
        /// inherit the color of the Tail Oversuit, we would do:
        /// cooltail -> cooltail-oversuit
        /// cooltail will be hidden from the color picker, and just use whatevers set for
        /// cooltail-oversuit. Easy huh?
        /// also, FLOOF ADD =3
        /// </summary>
        [DataField]
        public Dictionary<string, string>? ColorLinks { get; private set; }

        public Marking AsMarking()
        {
            return new Marking(ID, Sprites.Count);
        }

        // imp add
        /// <summary>
        /// Chance this marking will be added by appearance randomizer.
        /// </summary>
        /// <remarks>
        /// Default value is 1.
        /// </remarks>
        [DataField]
        public float RandomWeight = 1f;

    }
}

/* * * * * * * * * * * * * * * * * * * * * * * * *
 * HOW 2 MAKE MARKINGS WITH MULTIPLE LAYERS
 * by Dank Elly
 *
 * So, since the dawn of SS14, markings have been a single layer.
 * This is fine, since most markings are simple in terms of how they appear on a mob.
 * Just a cool skin color, or a pattern, or something.
 *
 * Then, tails were added.
 * Tails are different, if you think about it!
 * They tend to hang from the body, and appear behind the mob from some angles, and
 * in front of the mob from others. No real way around it, otherwise it'll look wierd!
 *
 * But, markings are still a single layer, which means that all the images sit in the
 * stacked deck of cards that is our sprite layers. If a marking is set to be in the
 * Underwear layer, that marking will only ever be in the Underwear layer, no matter
 * which way you turn! Fine and all for most markings, but not for tails.
 *
 * The previous solution was to just cookie-cut out a human shape from some directions
 * of the tail sprites, then layer then on top of the mob, over clothes and such.
 * This worked! Sort of! It wouldnt support missing legs, it only worked if you were
 * wearing skin-tight jumpsuits, and it made it so that you can't have any fancy leg
 * shapes (digilegs my beloved~)
 *
 * However, SS13 had solved this years ago, which I am totally taking credit for cus
 * I made roughly this system for Goonstation! The solution here was to just use
 * two layers lmao! A layer for the tail from most angles, and a layer for the tail
 * when youre facing away. This is known as the Behind and Oversuit layers, cus one
 * goes behind the mob, the other goes over your suit (and ideally under the backpack).
 *
 * LOREDUMP OVER HOW TO MAKE IT DO THIS
 *
 * First, make your marking prototype as normal.
 *   Note, you'll need extra sprites for every layer you want to add.
 *   This includes things like colorable accessories, which will need to be
 *   added to the new layers as well.
 * Next, add in a 'layering' entry to the marking prototype.
 * Then, add in a new entry to the 'layering' dictionary, like so:
 *   layering:
 *     name_of_image: LayerToPutItIn
 *     name_of_another_image: LayerToPutItIn
 *   I'll have a complete example later, but this is the basic idea.
 *   The first part of the entry is what you put for the state of that sprite
 *     Its how the game knows which sprite to mess with!
 *   The second part is the layer you want to put it in.
 *     This points to an entry in the enum stored in this file:
 *       Content.Shared/Humanoid/HumanoidVisualLayers.cs
 *     Capitalization matters!
 * todo: a way to link the colorations between layers
 *
 * Heres an example from Resources/Prototypes/Floof/Entities/Mobs/Customization/Markings/debug.yml
 *
- type: marking
  id: TailDebugPro
  bodyPart: Tail
  markingCategory: Tail
  speciesRestriction: [Reptilian, SlimePerson, IPC, Rodentia, Vulpkanin, Felinid, Human, Oni]
  layering:
    tail_oversuit: TailOversuit <--------------\
    tail_behind: TailBehind   <----------------+--------\
  sprites:                                     |        |
  - sprite: _Floof/Mobs/Customization/debug.rsi |        |
    state: tail_oversuit   >-------------------/        |
  - sprite: _Floof/Mobs/Customization/debug.rsi          |
    state: tail_behind   >------------------------------/
 *
 * (dont include the arrows lol)
 */
