#nullable enable
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

namespace Content.Client.GameObjects.Components
{
    public sealed class AcceptCloningWindow : SS14Window
    {
        private readonly ILocalizationManager _loc;

        private VBoxContainer MainVBox;

        public Button DenyButton;
        public readonly Button ConfirmButton;

        protected override Vector2 ContentsMinimumSize => MainVBox?.CombinedMinimumSize ?? Vector2.Zero;

        protected override Vector2? CustomSize => (250, 300);

        public AcceptCloningWindow(ILocalizationManager loc)
        {
            _loc = loc;

            Title = _loc.GetString("Cloning Machine");

            Contents.AddChild(MainVBox = new VBoxContainer
            {
                Children =
                {
                    new VBoxContainer
                    {
                        Children =
                        {
                            (new Label
                            {
                                Text = "You are being cloned! Transfer you soul to the clone body?"
                            }),
                            new HBoxContainer
                            {
                                Children =
                                {
                                    (ConfirmButton = new Button
                                    {
                                        Text = _loc.GetString("Yes"),
                                    }),
                                    (DenyButton = new Button
                                    {
                                        Text = _loc.GetString("No"),
                                    })
                                }
                            },
                        }
                    },
                }
            });
        }

        public void Close()
        {
            base.Close();

            Dispose();
        }
    }
}
