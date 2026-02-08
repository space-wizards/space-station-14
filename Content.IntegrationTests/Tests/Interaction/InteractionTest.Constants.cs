using Content.Shared.Stacks;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Interaction;

// This partial class contains various constant prototype IDs common to interaction tests.
// Should make it easier to mass-change hard coded strings if prototypes get renamed.
public abstract partial class InteractionTest
{
    /// <summary>
    /// Prototype for a prying tool with strength "forced", which is used by zombies and some animals.
    /// They can pry bolted doors.
    /// </summary>
    [TestPrototypes]
    private static readonly string ForcedPryerPrototype = $"""
        - type: entity
          name: super jaws of life
          id: SuperJawsOfLife
          components:
          - type: Item
            size: Normal
          - type: Tool
            qualities:
            - Prying
          - type: Prying
            speedModifier: 1.5
            strength: Forced
        """;

    // Tiles
    protected const string Floor = "FloorSteel";
    protected const string FloorItem = "FloorTileItemSteel";
    protected const string Plating = "Plating";
    protected const string PlatingRCD = "PlatingRCD";
    protected const string Lattice = "Lattice";
    protected const string PlatingBrass = "PlatingBrass";

    // Structures
    protected const string Airlock = "Airlock";
    protected const string Turnstile = "Turnstile";

    // Tools/steps
    protected const string Wrench = "Wrench";
    protected const string Screw = "Screwdriver";
    protected const string Weld = "WelderExperimental";
    protected const string Pry = "Crowbar";
    protected const string Cut = "Wirecutter";
    protected const string PryPowered = "JawsOfLife";
    protected const string ForcedPryer = "SuperJawsOfLife";

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

    // Inflatables & Needle used to pop them
    protected static readonly EntProtoId InflatableWall = "InflatableWall";
    protected static readonly EntProtoId Needle = "WeaponMeleeNeedle";
    protected static readonly ProtoId<StackPrototype> InflatableWallStack = "InflatableWall";
}
