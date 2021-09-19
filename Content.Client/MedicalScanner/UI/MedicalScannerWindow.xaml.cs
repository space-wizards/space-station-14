using System.Text;
using System.Collections.Generic;
using System.Linq;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using static Content.Shared.MedicalScanner.SharedMedicalScannerComponent;
using static Robust.Client.UserInterface.Controls.BoxContainer;
using Content.Shared.Damage.Prototypes;

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

                var totalDamage = state.DamagePerType.Values.Sum();
                text.Append($"{Loc.GetString("medical-scanner-window-entity-damage-total-text", ("amount", totalDamage))}\n");

                HashSet<string> shownTypes = new();

                // Show the total damage and type breakdown for each damage group.
                foreach (var (damageGroupID, damageAmount) in state.DamagePerGroup)
                {
                    text.Append($"\n{Loc.GetString("medical-scanner-window-damage-group-text", ("damageGroup", damageGroupID), ("amount", damageAmount))}");

                    // Show the damage for each type in that group.
                    var group = IoCManager.Resolve<IPrototypeManager>().Index<DamageGroupPrototype>(damageGroupID);
                    foreach (var type in group.DamageTypes)
                    {
                        if (state.DamagePerType.TryGetValue(type, out var typeAmount))
                        {
                            // If damage types are allowed to belong to more than one damage group, they may appear twice here. Mark them as duplicate.
                            if (!shownTypes.Contains(type))
                            {
                                shownTypes.Add(type);
                                text.Append($"\n- {Loc.GetString("medical-scanner-window-damage-type-text", ("damageType", type), ("amount", typeAmount))}");
                            }
                            else {
                                text.Append($"\n- {Loc.GetString("medical-scanner-window-damage-type-duplicate-text", ("damageType", type), ("amount", typeAmount))}");
                            }
                        }
                    }
                    text.Append('\n');
                }

                _diagnostics.Text = text.ToString();
                ScanButton.Disabled = state.IsScanned;

                // TODO MEDICALSCANNER resize window based on the length of text / number of damage types?
                // Also, maybe add color schemes for specific damage groups?
                SetSize = (250, 600);
            }
        }
    }
}
