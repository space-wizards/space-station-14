using Content.Client.Creatures.SpaceLeech.UI;
using Content.Shared.Creatures.SpaceLeech;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Creatures.SpaceLeech;

[UsedImplicitly]
public sealed class SpaceLeechUpgradeMenuBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private SpaceLeechUpgradeMenuWindow? _window;

    public SpaceLeechUpgradeMenuBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<SpaceLeechUpgradeMenuWindow>();
        _window.OnEvolve += upgradeId => SendMessage(new SpaceLeechEvolveMessage(upgradeId));

        Refresh();
    }

    /// <summary>
    /// Re-reads the networked <see cref="SpaceLeechComponent"/> into the window.
    /// Called on open and by <see cref="SpaceLeechUiSystem"/> whenever new state arrives.
    /// </summary>
    public void Refresh()
    {
        if (EntMan.TryGetComponent(Owner, out SpaceLeechComponent? comp))
            _window?.Update(comp);
    }
}
