namespace Content.IntegrationTests.Tests.Interaction;

// This partial class contains various constant prototype IDs common to interaction tests.
// Should make it easier to mass-change hard coded strings if prototypes get renamed.
public abstract partial class InteractionTest
{
    // Tiles
    protected const string Floor = "FloorSteel";
    protected const string FloorItem = "FloorTileItemSteel";
    protected const string Plating = "Plating";
    protected const string Lattice = "Lattice";

    // Structures
    protected const string Airlock = "Airlock";

    // Tools/steps
    protected const string Wrench = "Wrench";
    protected const string Screw = "Screwdriver";
    protected const string Weld = "WelderExperimental";
    protected const string Pry = "Crowbar";
    protected const string Cut = "Wirecutter";

    // Materials/stacks
    protected const string Steel = "Steel";
    protected const string Glass = "Glass";
    protected const string RGlass = "ReinforcedGlass";
    protected const string Plastic = "Plastic";
    protected const string Cable = "Cable";
    protected const string Rod = "MetalRod";

    // Parts
    protected const string Manipulator1 = "MicroManipulatorStockPart";
    protected const string Battery1 = "PowerCellSmall";
    protected const string Battery4 = "PowerCellHyper";
}
