using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Systems.Medical.Controls;

public sealed class MedicalDoll : Control
{
    public readonly SpriteView TargetDummy;
    public readonly Label TargetName;


    public MedicalDoll()
    {
        TargetDummy = new SpriteView()
        {
            HorizontalAlignment = HAlignment.Center,
        };
        TargetName = new Label()
        {
            Text = "Self",
            Align = Label.AlignMode.Center,
            HorizontalAlignment = HAlignment.Center,
        };
        AddChild(TargetName);
        AddChild(TargetDummy);
    }

    public void UpdateUI(SpriteComponent? targetSprite, string? identity)
    {
        TargetDummy.Sprite = targetSprite;
        if (targetSprite == null)
        {
            TargetName.Text = "Error No Target";
            return;
        }
        TargetName.Text = identity != null ? "Self" : identity;
    }
}
