using Content.Shared.Humanoid.Markings;
using Robust.Shared.Serialization;

namespace Content.Shared.Humanoid
{
    /// <summary>
    ///  These are the layer defines for the humanoid sprite system.
    ///  If you want to add a new layer slot to species? Scroll down~
    /// </summary>
    [Serializable, NetSerializable]
    public enum HumanoidVisualLayers : byte
    {
        Special, // for the cat ears
        Tail,
        Hair,
        FacialHair,
        UndergarmentTop,
        UndergarmentBottom,
        Chest,
        Head,
        Snout,
        HeadSide, // side parts (i.e., frills)
        HeadTop,  // top parts (i.e., ears)
        TailBehind, // FLOOF - add tails that dont have to go through a brutal cookiecutter to work
        TailBehindBackpack, // imp - to layer behind backpacks, treat this like an oversuit
        TailOversuit, // FLOOF - add tails that dont have to go through a brutal cookiecutter to work
        TailUnderlay, // imp - temporary until i have a better way to do two part tails
        Eyes,
        RArm,
        LArm,
        RHand,
        LHand,
        RLeg,
        LLeg,
        RFoot,
        LFoot,
        Handcuffs,
        StencilMask,
        Ensnare,
        Fire,
        LArmExtension, // Frontier: a species-specific extension layer, e.g. for harpy wings
        RArmExtension, // Frontier: a species-specific extension layer, e.g. for harpy wings

    }
}

/* * * * * * * * * * * * * * * * * * * * * * * * *
 * HOW 2 ADD A NEW LAYER FOR MOB NON-EQUIPMENT APPEARANCE STUFF
 * by Dank Elly
 * Lets say you want to add something like socks to be selectable through markings
 * Since people tend to have two feet, you'll actually need two new layers! one for each foot.
 * First, add in the enum above a new layer slot, like so:
 *     LSock,
 *     RSock,
 *   Easy huh? Note that this enum does *not* define which layer in relation to other layers it will be layered!
 * Next, since these'll be a whole new marking type, we'll need to add in a new marking category.
 *   in Content.Shared/Humanoid/Markings/MarkingCategories.cs, add in a new category for the socks
 *   lets assume we'll let the player pick two different socks, which means two different categories
 *   Its done the same way as this enum here
 * Next, go to Content.Shared/Humanoid/HumanoidVisualLayersExtension.cs
 *   In here, find Sublayers() and add in the new layers to the switch statement
 *     case HumanoidVisualLayers.LFoot:
 *     yield return HumanoidVisualLayers.LSock;
 *   and so on. This will make it so if your leg/foot stops being visible, the sock will vanish as well! i think
 * Next, we'll have to go through a lot of species files, to change a few things:
 *   speciesBaseSprites
 *     I'm not sure what this does, but you'll want to add in an entry under sprites for the new layer
 *       LSock: MobHumanoidAnyMarking <- I think this makes it selectable in the editor, and blank by default?
 *       RSock: MobHumanoidAnyMarking
 *   layers
 *     This is part of the species entity prototype, and this is where the sprite layering is defined.
 *     Note that this includes equipment layers, which means you can totally, say, have a tail sit between your suit and your backpack!
 *       - map: [ "enum.HumanoidVisualLayers.LFoot" ]
 *       - map: [ "enum.HumanoidVisualLayers.LSock" ]
 *       - map: [ "enum.HumanoidVisualLayers.RFoot" ]
 *       - map: [ "enum.HumanoidVisualLayers.RSock" ]
 *     This will make it so the socks are drawn on top of the feet, and not behind them.
 *   I'd list which files these are in, but there are a million different species files, all with the same name, in different folders
 *   So just search for "speciesBaseSprites" and "enum.HumanoidVisualLayers.Chest" and you'll find them all
 *     If you miss some, dont sweat it, someone'll point it out and then you can fix it! Hotties like you make the hottest hotfixes~
 */
