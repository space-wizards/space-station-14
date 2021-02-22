using Content.Shared.GameObjects.Components.Disposal;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using static Content.Shared.GameObjects.Components.Disposal.SharedDisposalTaggerComponent;

namespace Content.Client.GameObjects.Components.Disposal
{
    /// <summary>
    /// Client-side UI used to control a <see cref="SharedDisposalTaggerComponent"/>
    /// </summary>
    public class DisposalTaggerWindow : SS14Window
    {
        public readonly LineEdit TagInput;
        public readonly Button Confirm;

        public DisposalTaggerWindow()
        {
            MinSize = SetSize = (400, 80);
            Title = Loc.GetString("Disposal Tagger");

            Contents.AddChild(new VBoxContainer
            {
                Children =
                {
                    new Label {Text = Loc.GetString("Tag:")},
                    new Control {MinSize = (0, 10)},
                    new HBoxContainer
                    {
                        Children =
                        {
                            (TagInput = new LineEdit
                            {
                                HorizontalExpand = true,
                                MinSize = (320, 0),
                                IsValid = tag => TagRegex.IsMatch(tag)
                            }),
                            new Control {MinSize = (10, 0)},
                            (Confirm = new Button {Text = Loc.GetString("Confirm")})
                        }
                    }
                }
            });
        }


        public void UpdateState(DisposalTaggerUserInterfaceState state)
        {
            TagInput.Text = state.Tag;
        }
    }
}
