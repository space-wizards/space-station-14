namespace Content.Shared._Offbrand.Medical;

/// <summary>
/// Raised on an entity to get its stethoscope sounds.
/// </summary>
/// <param name="Messages">The list of sounds to report.</param>
[ByRefEvent]
public readonly record struct StethoscopeExamineEvent(List<string> Messages);
