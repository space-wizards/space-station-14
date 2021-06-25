using Content.Shared.Disposal.Components;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using static Content.Shared.Disposal.Components.SharedDisposalTaggerComponent;

namespace Content.Client.Disposal.UI
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
            MinSize = SetSize = (500, 110);
            Title = Loc.GetString("disposal-tagger-window-title");

            Contents.AddChild(new VBoxContainer
            {
                Children =
                {
                    new Label {Text = Loc.GetString("disposal-tagger-window-tag-input-label")},
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
                            (Confirm = new Button {Text = Loc.GetString("disposal-tagger-window-tag-confirm-button")})
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
