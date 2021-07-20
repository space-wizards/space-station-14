using System.Text;
using Content.Shared.Damage;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using static Content.Shared.MedicalScanner.SharedMedicalScannerComponent;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.MedicalScanner.UI
{
    public class MedicalScannerWindow : SS14Window
    {
        public readonly Button ScanButton;
        private readonly Label _diagnostics;
        public MedicalScannerWindow()
        {
            SetSize = (250, 100);

            Contents.AddChild(new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                Children =
                {
                    (ScanButton = new Button
                    {
                        Text = Loc.GetString("medical-scanner-window-save-button-text")
                    }),
                    (_diagnostics = new Label
                    {
                        Text = string.Empty
                    })
                }
            });
        }

        public void Populate(MedicalScannerBoundUserInterfaceState state)
        {
            var text = new StringBuilder();

            if (!state.Entity.HasValue ||
                !state.HasDamage() ||
                !IoCManager.Resolve<IEntityManager>().TryGetEntity(state.Entity.Value, out var entity))
            {
                _diagnostics.Text = Loc.GetString("medical-scanner-window-no-patient-data-text");
                ScanButton.Disabled = true;
                SetSize = (250, 100);
            }
            else
            {
                text.Append($"{Loc.GetString("medical-scanner-window-entity-health-text", ("entityName", entity.Name))}\n");

                foreach (var (@class, classAmount) in state.DamageClasses)
                {
                    text.Append($"\n{Loc.GetString("medical-scanner-window-damage-class-text", ("damageClass", @class), ("amount", classAmount))}");

                    foreach (var type in @class.ToTypes())
                    {
                        if (!state.DamageTypes.TryGetValue(type, out var typeAmount))
                        {
                            continue;
                        }

                        text.Append($"\n- {Loc.GetString("medical-scanner-window-damage-type-text", ("damageType",type) ,("amount", typeAmount))}");
                    }

                    text.Append('\n');
                }

                _diagnostics.Text = text.ToString();
                ScanButton.Disabled = state.IsScanned;

                SetSize = (250, 575);
            }
        }
    }
}
