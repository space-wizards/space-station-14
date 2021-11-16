using System;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Administration.UI.CustomControls;

public class AdminLogLabel : Label
{
    public AdminLogLabel(int id)
    {
        Id = id;
    }

    public int Id { get; set; }
}
