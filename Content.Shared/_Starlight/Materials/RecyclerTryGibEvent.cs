namespace Content.Shared._Starlight.Materials;

[ByRefEvent]
public record struct RecyclerTryGibEvent(EntityUid Victim, bool Handled = false);