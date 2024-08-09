namespace Content.Shared.Procedural.PostGeneration;

/// <summary>
/// If internal areas are found will try to generate windows.
/// </summary>
/// <remarks>
/// Dungeon data keys are:
/// - FallbackTile
/// - Window
/// </remarks>
public sealed partial class InternalWindowDunGen : IDunGenLayer;
