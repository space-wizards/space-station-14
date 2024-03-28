using System.Numerics;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Preferences.Loadouts.Effects;
using Content.Shared.Roles;
using Content.Shared.StatusIcon;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.Prototypes;

namespace Content.Client.Preferences.UI;

public sealed class JobPrioritySelector : RequirementsSelector<JobPrototype>
{
    public JobPriority Priority
    {
        get => (JobPriority) Options.SelectedValue;
        set => Options.SelectByValue((int) value);
    }

    public event Action<JobPriority>? PriorityChanged;

    public JobPrioritySelector(RoleLoadout? loadout, JobPrototype proto, ButtonGroup btnGroup, IPrototypeManager protoMan)
        : base(proto, btnGroup)
    {
        Options.OnItemSelected += args => PriorityChanged?.Invoke(Priority);

        var items = new[]
        {
            ("humanoid-profile-editor-job-priority-high-button", (int) JobPriority.High),
            ("humanoid-profile-editor-job-priority-medium-button", (int) JobPriority.Medium),
            ("humanoid-profile-editor-job-priority-low-button", (int) JobPriority.Low),
            ("humanoid-profile-editor-job-priority-never-button", (int) JobPriority.Never),
        };

        var icon = new TextureRect
        {
            TextureScale = new Vector2(2, 2),
            VerticalAlignment = VAlignment.Center
        };
        var jobIcon = protoMan.Index<StatusIconPrototype>(proto.Icon);
        icon.Texture = jobIcon.Icon.Frame0();

        Setup(loadout, items, proto.LocalizedName, 200, proto.LocalizedDescription, icon);
    }
}
