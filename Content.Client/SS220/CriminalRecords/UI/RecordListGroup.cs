// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.SS220.CriminalRecords.UI;

[Virtual]
public abstract partial class RecordListGroup : BoxContainer
{
    public Control RecordContainer;
    public RecordListGroup()
    {
        RecordContainer = this;
    }

    public string GroupName { get; protected set; } = "";
    protected Color Color = Color.White;

    public void Setup(string name, Color? color = null)
    {
        if (color.HasValue)
            Color = color.Value;

        SetGroupName(name);
    }

    public virtual void SetGroupName(string name)
    {
        GroupName = name;
    }
}
