using Content.Shared.Ghost.Roles;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.Ghost.Roles.UI;

public sealed class GhostRoleEntry : BaseEntry
{
    public event Action<GhostRoleInfo>? OnRoleTake;
    public event Action<GhostRoleInfo>? OnRoleSelected;
    public event Action<GhostRoleInfo>? OnRoleCancelled;
    public event Action<GhostRoleInfo>? OnRoleFollowed;

    public GhostRoleEntry(GhostRoleInfo role, bool isRequested)
    {
        var total = role.AvailableLotteryRoleCount + role.AvailableImmediateRoleCount;
        var showButtonNumbers = role.AvailableImmediateRoleCount > 0 && role.AvailableLotteryRoleCount > 0;

        Title.Text = total > 1 ? $"{role.Name} ({total})" : role.Name;
        Description.SetMessage(role.Description);

        TakeButton.Text = showButtonNumbers ? $"Take ({role.AvailableImmediateRoleCount})" : "Take";
        RequestButton.Text = showButtonNumbers ? $"Request ({role.AvailableLotteryRoleCount})" : "Request";

        TakeButton.Visible = role.AvailableImmediateRoleCount > 0;
        RequestButton.Visible = role.AvailableLotteryRoleCount > 0 && !isRequested;
        CancelButton.Visible = role.AvailableLotteryRoleCount > 0 && isRequested;

        TakeButton.OnPressed += _ => OnRoleTake?.Invoke(role);
        RequestButton.OnPressed += _ => OnRoleSelected?.Invoke(role);
        CancelButton.OnPressed += _ => OnRoleCancelled?.Invoke(role);
        FollowButton.OnPressed += _ => OnRoleFollowed?.Invoke(role);
    }
}
