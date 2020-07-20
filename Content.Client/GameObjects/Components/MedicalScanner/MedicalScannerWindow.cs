using System.Text;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Maths;
using static Content.Shared.GameObjects.Components.Medical.SharedMedicalScannerComponent;

namespace Content.Client.GameObjects.Components.MedicalScanner
{
    public class MedicalScannerWindow : SS14Window
    {
        protected override Vector2? CustomSize => (485, 90);

        public void Populate(MedicalScannerBoundUserInterfaceState state)
        {
            Contents.RemoveAllChildren();
            var text = new StringBuilder();
            if (state.MaxHealth == 0)
            {
                text.Append("No patient data.");
            } else
            {
                text.Append($"Patient's health: {state.CurrentHealth}/{state.MaxHealth}\n");

                if (state.DamageDictionary != null)
                {
                    foreach (var (dmgType, amount) in state.DamageDictionary)
                    {
                        text.Append($"\n{dmgType}: {amount}");
                    }
                }
            }
            Contents.AddChild(new Label(){Text = text.ToString()});
        }
    }
}
