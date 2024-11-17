using Robust.Client.UserInterface.Controls;

namespace Content.Client.Administration.UI.CustomControls;

public sealed class AdminLogPlayerButton : Button
{
    public AdminLogPlayerButton(Guid id)
    {
        Id = id;
        ClipText = true;
        ToggleMode = true;
    }

    public Guid Id { get; }
}
