using Content.Shared.Radio;
using Robust.Client.GameObjects;

namespace Content.Client.Radio;

public sealed class RadioBoundUserInterface : BoundUserInterface
{
    // [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    [ViewVariables]
    private UI.RadioWindow? _menu;

    public RadioBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _menu = new UI.RadioWindow(this) {Title = _entityManager.GetComponent<MetaDataComponent>(Owner.Owner).EntityName};

        _menu.OnClose += Close;
        _menu.OpenCentered();
    }

    public void ToggleFrequencyFilter(int frequency)
    {
        SendMessage(new RadioToggleFrequencyFilter
        {
            Frequency = frequency
        });
    }

    public void ChangeFrequency(int frequency)
    {
        SendMessage(new RadioChangeFrequency
        {
            Frequency = frequency
        });
    }

    public void ToggleRX()
    {
        SendMessage(new RadioToggleRX());
    }

    public void ToggleTX()
    {
        SendMessage(new RadioToggleTX());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        var castState = (RadioBoundInterfaceState) state;
        _menu?.UpdateState(castState);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;
        _menu?.Dispose();
    }
}
