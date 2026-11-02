using System.Linq;
using System.Numerics;
using Content.Client.Lobby.UI.Roles;
using Content.Shared.CF.Character;
using Content.Shared.Clothing;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    /// <summary>
    /// Temporary override of their selected job, used to preview role loadouts.
    /// </summary>
    public JobPrototype? JobOverride;

    private readonly List<(string, RequirementsSelector)> _jobPriorities = new();
    private readonly Dictionary<string, BoxContainer> _jobCategories = new();

    /// <summary>
    /// Refresh the equipment tabs from the current role loadout.
    /// </summary>
    public void RefreshLoadouts()
    {
        RefreshEquipmentTabs();
    }

    private void UpdateJobPriorities()
    {
        foreach (var (jobId, prioritySelector) in _jobPriorities)
        {
            var priority = Profile?.JobPriorities.GetValueOrDefault(jobId, JobPriority.Never) ?? JobPriority.Never;
            prioritySelector.Select((int)priority);
        }
    }

    /// <summary>
    /// Refreshes the character-builder job list. Only builder job groups are shown.
    /// </summary>
    public void RefreshJobs()
    {
        JobList.RemoveAllChildren();
        _jobCategories.Clear();
        _jobPriorities.Clear();
        var firstCategory = true;

        var groups = _prototypeManager.EnumeratePrototypes<CFJobGroupPrototype>()
            .OrderByDescending(group => group.Weight)
            .ThenBy(group => Loc.GetString(group.Name), StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var items = new[]
        {
            ("humanoid-profile-editor-job-priority-never-button", (int)JobPriority.Never),
            ("humanoid-profile-editor-job-priority-low-button", (int)JobPriority.Low),
            ("humanoid-profile-editor-job-priority-medium-button", (int)JobPriority.Medium),
            ("humanoid-profile-editor-job-priority-high-button", (int)JobPriority.High),
        };

        foreach (var group in groups)
        {
            var departmentName = Loc.GetString(group.Name);
            var category = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                Name = $"JobGroup{group.ID}",
                ToolTip = Loc.GetString("humanoid-profile-editor-jobs-amount-in-department-tooltip",
                    ("departmentName", departmentName)),
            };

            if (!firstCategory)
            {
                category.AddChild(new Control
                {
                    MinSize = new Vector2(0, 23),
                });
            }

            firstCategory = false;
            category.AddChild(new PanelContainer
            {
                PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#464966") },
                Children =
                {
                    new Label
                    {
                        Text = departmentName,
                        Margin = new Thickness(5f, 0, 0, 0),
                    },
                },
            });

            _jobCategories[group.ID] = category;
            JobList.AddChild(category);

            foreach (var jobId in group.Roles)
            {
                if (!_prototypeManager.TryIndex(jobId, out var job) || !job.SetPreference)
                    continue;

                var jobContainer = new BoxContainer
                {
                    Name = $"JobRow{job.ID}",
                    Orientation = LayoutOrientation.Horizontal,
                };

                var selector = new RequirementsSelector
                {
                    Margin = new Thickness(3f, 3f, 3f, 0f),
                };
                selector.OnOpenGuidebook += OnOpenGuidebook;

                var icon = new TextureRect
                {
                    TextureScale = new Vector2(2, 2),
                    VerticalAlignment = VAlignment.Center,
                };
                var jobIcon = _prototypeManager.Index(job.Icon);
                icon.Texture = _sprite.Frame0(jobIcon.Icon);
                selector.Setup(items, job.LocalizedName, 200, job.LocalizedDescription, icon, job.Guides);

                if (!_requirements.IsAllowed(job, (HumanoidCharacterProfile?)_preferencesManager.Preferences?.SelectedCharacter, out var reason))
                    selector.LockRequirements(reason);
                else
                    selector.UnlockRequirements();

                selector.OnSelected += selectedPrio =>
                {
                    var selectedJobPrio = (JobPriority)selectedPrio;
                    Profile = Profile?.WithJobPriority(job.ID, selectedJobPrio);

                    foreach (var (otherJobId, other) in _jobPriorities)
                    {
                        if (otherJobId == job.ID)
                        {
                            other.Select(selectedPrio);
                            continue;
                        }

                        if (selectedJobPrio != JobPriority.High || (JobPriority)other.Selected != JobPriority.High)
                            continue;

                        other.Select((int)JobPriority.Medium);
                        Profile = Profile?.WithJobPriority(otherJobId, JobPriority.Medium);
                    }

                    if (selectedJobPrio == JobPriority.High)
                        JobOverride = null;

                    ReloadPreview();
                    RefreshEquipmentTabs();
                    UpdateJobPriorities();
                    UpdateBuilderSummary();
                    SetDirty();
                };

                var loadoutWindowBtn = new Button
                {
                    Text = Loc.GetString("cf-character-builder-edit-equipment"),
                    HorizontalAlignment = HAlignment.Right,
                    VerticalAlignment = VAlignment.Center,
                    Margin = new Thickness(3f, 3f, 0f, 0f),
                };

                if (!_prototypeManager.TryIndex<RoleLoadoutPrototype>(LoadoutSystem.GetJobPrototype(job.ID), out _))
                    loadoutWindowBtn.Disabled = true;
                else
                    loadoutWindowBtn.OnPressed += _ => FocusGearTab(job);

                _jobPriorities.Add((job.ID, selector));
                jobContainer.AddChild(selector);
                jobContainer.AddChild(loadoutWindowBtn);
                category.AddChild(jobContainer);
            }
        }

        UpdateJobPriorities();
    }

    /// <summary>
    /// Antagonist preferences stay on the profile. The builder does not show that list.
    /// </summary>
    public void RefreshAntags()
    {
    }
}
