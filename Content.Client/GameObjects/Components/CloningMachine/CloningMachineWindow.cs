using System.Text;
using Content.Shared.Damage;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using static Content.Shared.GameObjects.Components.Medical.SharedCloningMachineComponent;

namespace Content.Client.GameObjects.Components.CloningMachine
{
    public class CloningMachineWindow : SS14Window
    {
        public readonly Button ScanButton;
        private readonly Label _diagnostics;
        protected override Vector2? CustomSize => (485, 90);

        public CloningMachineWindow()
        {
            Contents.AddChild(new VBoxContainer
            {
                Children =
                {
                    (ScanButton = new Button
                    {
                        Text = "Clone"
                    }),
                    (_diagnostics = new Label
                    {
                        Text = ""
                    })
                }
            });
        }

        public void Populate(CloningMachineBoundUserInterfaceState state)
        {

        }
    }
}
