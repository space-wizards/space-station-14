using System.Text;
using System.Collections.Generic;
using Content.Shared.Damage;
using System.Linq;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
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

<<<<<<< HEAD
<<<<<<< refs/remotes/origin/master
                foreach (var (@class, classAmount) in state.DamageClasses)
=======
                // Show the total damage
                var totalDamage = state.DamagePerTypeID.Values.Sum();
                text.Append($"{Loc.GetString("medical-scanner-window-entity-damage-total-text", ("amount", totalDamage))}\n");

                // Keep track of how many damage types we have shown
                HashSet<string> shownTypeIDs = new();

                // First show just the total damage and type breakdown for each damge group that is fully supported by that entitygroup.
                foreach (var (damageGroupID, damageAmount) in state.DamagePerSupportedGroupID)
>>>>>>> refactor-damageablecomponent
                {

<<<<<<< HEAD
                    foreach (var type in @class.ToTypes())
=======
                // Show the total damage
                var totalDamage = state.DamagePerTypeID.Values.Sum();
                text.Append($"{Loc.GetString("medical-scanner-window-entity-damage-total-text", ("amount", totalDamage))}\n");

                // Keep track of how many damage types we have shown
                HashSet<string> shownTypeIDs = new();

                // First show just the total damage and type breakdown for each damge group that is fully supported by that entitygroup.
                foreach (var (damageGroupID, damageAmount) in state.DamagePerSupportedGroupID)
                {

=======
>>>>>>> refactor-damageablecomponent
                    // Show total damage for the group
                    text.Append($"\n{Loc.GetString("medical-scanner-window-damage-group-text", ("damageGroup", damageGroupID), ("amount", damageAmount))}");

                    // Then show the damage for each type in that group.
                    // currently state has a dictionary mapping groupsIDs to damage, and typeIDs to damage, but does not know how types and groups are related.
                    // So use PrototypeManager.
                    var group = IoCManager.Resolve<IPrototypeManager>().Index<DamageGroupPrototype>(damageGroupID);
                    foreach (var type in group.DamageTypes)
<<<<<<< HEAD
>>>>>>> Refactor damageablecomponent update (#4406)
=======
>>>>>>> refactor-damageablecomponent
                    {
                        if (state.DamagePerTypeID.TryGetValue(type.ID, out var typeAmount))
                        {
                            // If damage types are allowed to belong to more than one damage group, they may appear twice here. Mark them as duplicate.
                            if (!shownTypeIDs.Contains(type.ID))
                            {
                                shownTypeIDs.Add(type.ID);
                                text.Append($"\n- {Loc.GetString("medical-scanner-window-damage-type-text", ("damageType", type.ID), ("amount", typeAmount))}");
                            }
                            else {
                                text.Append($"\n- {Loc.GetString("medical-scanner-window-damage-type-duplicate-text", ("damageType", type.ID), ("amount", typeAmount))}");
                            }
                        }
                    }
                    text.Append('\n');
                }

                // Then, lets also list any damageType that was not fully Supported by the entity's damageContainer
                var textAppendix = new StringBuilder();
                int totalMiscDamage = 0;
                // Iterate over ids that have not been printed.
                foreach (var damageTypeID in state.DamagePerTypeID.Keys.Where(typeID => !shownTypeIDs.Contains(typeID)))
                {
                     //This damage type was not yet added to the text.
                     textAppendix.Append($"\n- {Loc.GetString("medical-scanner-window-damage-type-text", ("damageType", damageTypeID), ("amount", state.DamagePerTypeID[damageTypeID]))}");
                     totalMiscDamage += state.DamagePerTypeID[damageTypeID];
                }

                // Is there any information to show? Did any damage types not belong to a group?
                if (textAppendix.Length > 0) {
                    text.Append($"\n{Loc.GetString("medical-scanner-window-damage-group-text", ("damageGroup", "Miscellaneous"), ("amount", totalMiscDamage))}");
                    text.Append(textAppendix);
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
