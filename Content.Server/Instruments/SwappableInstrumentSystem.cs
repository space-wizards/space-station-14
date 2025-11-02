namespace Content.Shared.Instruments;

/// <summary>
/// Shared system that manages swapping between available instrument sound sets.
/// </summary>
public sealed class SharedSwappableInstrumentSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SwappableInstrumentComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, SwappableInstrumentComponent component, ComponentInit args)
    {
        // Ensure the selected index is valid at initialization.
        if (component.AvailablePrograms.Count == 0)
            component.CurrentIndex = 0;
        else if (component.CurrentIndex >= component.AvailablePrograms.Count)
            component.CurrentIndex = 0;
    }

    /// <summary>
    /// Cycles to the next available sound program.
    /// </summary>
    public void SwapNext(EntityUid uid, SwappableInstrumentComponent component)
    {
        if (component.AvailablePrograms.Count == 0)
            return;

        component.CurrentIndex = (component.CurrentIndex + 1) % component.AvailablePrograms.Count;
        Dirty(uid, component);
    }
}
