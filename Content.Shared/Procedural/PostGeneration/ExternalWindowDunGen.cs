namespace Content.Shared.Procedural.PostGeneration;

/// <summary>
/// If external areas are found will try to generate windows.
/// </summary>
/// <remarks>
/// Dungeon data keys are:
/// - EntranceFlank
/// - FallbackTile
/// </remarks>
public sealed partial class ExternalWindowDunGen : IDunGenLayer;
