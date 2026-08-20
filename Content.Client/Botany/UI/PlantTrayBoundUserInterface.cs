using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Botany.UI;

/// <summary>
/// Displays the plant tray user interface for an entity.
/// </summary>
[UsedImplicitly]
public sealed class PlantTrayBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private PlantTrayWindow? _window;

    /// <inheritdoc />
    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<PlantTrayWindow>();
        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;
        Update();
    }

    /// <inheritdoc />
    public override void Update()
    {
        _window?.Update(Owner);
    }
}
