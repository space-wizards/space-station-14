using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;

namespace Content.Shared.Chemistry.Events;

[ByRefEvent, Obsolete("This is for backwards compatibility only, and will be removed in the future. " +
                      "Do not use this for new content.")]
public record struct LegacySolutionAddedEvent(Solution Solution, ReagentQuantity ReagentQuantity);

[ByRefEvent, Obsolete("This is for backwards compatibility only, and will be removed in the future. " +
                      "Do not use this for new content.")]
public record struct LegacySolutionRemovedEvent(Solution Solution, ReagentQuantity ReagentQuantity);

[ByRefEvent, Obsolete("This is for backwards compatibility only, and will be removed in the future. " +
                      "Do not use this for new content.")]
public record struct LegacySolutionSetMaxVolumeEvent(Solution Solution, FixedPoint2 NewMax);

[ByRefEvent, Obsolete("This is for backwards compatibility only, and will be removed in the future. " +
                      "Do not use this for new content.")]
public record struct LegacySolutionScale(Solution Solution, float Factor);

[ByRefEvent, Obsolete("This is for backwards compatibility only, and will be removed in the future. " +
                      "Do not use this for new content.")]
public record struct LegacySolutionSetCanReactEvent(Solution Solution, bool CanReact);

[ByRefEvent, Obsolete("This is for backwards compatibility only, and will be removed in the future. " +
                      "Do not use this for new content.")]
public record struct LegacySolutionCheckCanReactEvent(Solution Solution, bool CanReact);

[ByRefEvent, Obsolete("This is for backwards compatibility only, and will be removed in the future. " +
                      "Do not use this for new content.")]
public record struct LegacySolutionSetCanTempEvent(Solution Solution, float SetTemperature);

[ByRefEvent, Obsolete("This is for backwards compatibility only, and will be removed in the future. " +
                      "Do not use this for new content.")]
public record struct LegacySolutionCheckCanAdd(Solution Solution, bool CanAdd);

[ByRefEvent, Obsolete("This is for backwards compatibility only, and will be removed in the future. " +
                      "Do not use this for new content.")]
public record struct LegacySolutionGetHeatCapacity(Solution Solution, float HeatCapacity);

[ByRefEvent, Obsolete("This is for backwards compatibility only, and will be removed in the future. " +
                      "Do not use this for new content.")]
public record struct LegacySolutionContainsReagent(Solution Solution, ReagentDef Reagent);

[ByRefEvent, Obsolete("This is for backwards compatibility only, and will be removed in the future. " +
                      "Do not use this for new content.")]
public record struct LegacySolutionTryGetReagent(Solution Solution, ReagentDef Reagent, bool Success, ReagentQuantity ReagentQuantity);

[ByRefEvent, Obsolete("This is for backwards compatibility only, and will be removed in the future. " +
                      "Do not use this for new content.")]
public record struct LegacySolutionGetReagentOfType(Solution Solution, string Reagent, ReagentQuantity[] ReagentQuantites);

[ByRefEvent, Obsolete("This is for backwards compatibility only, and will be removed in the future. " +
                      "Do not use this for new content.")]
public record struct LegacySolutionTryGetPrimaryReagent(Solution Solution, ReagentDef Reagent, bool Success, ReagentQuantity ReagentQuantity);

[ByRefEvent, Obsolete("This is for backwards compatibility only, and will be removed in the future. " +
                      "Do not use this for new content.")]
public record struct LegacySolutionClearAll(Solution Solution);
