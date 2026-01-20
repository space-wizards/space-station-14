using Content.Client.Lobby.UI.Loadouts;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;

namespace Content.Client.Lobby.UI.ProfileEditor.Tabs;

// Yeah fuck it let's take advantage of partial controls woohoo
public sealed partial class ProfileEditorJobsTab
{
    private LoadoutWindow? _loadoutWindow;

    protected override void ExitedTree()
    {
        base.ExitedTree();
        _loadoutWindow?.Close();
        _loadoutWindow = null;
    }

    private void OnJobLoadoutPressed(JobPrototype job, string jobID, RoleLoadoutPrototype roleLoadout)
    {
        RoleLoadout? loadout = null;

        if (_profile?.Loadouts.TryGetValue(jobID, out var existingLoadout) == true)
            loadout = existingLoadout.Clone();

        if (loadout is null)
        {
            loadout = new RoleLoadout(roleLoadout.ID);
            loadout.SetDefault(_profile, _playerManager.LocalSession, _prototypeManager);
        }

        OpenLoadout(job, loadout, roleLoadout);
    }

    /// <summary>
    /// Refresh all loadouts.
    /// </summary>
    public void RefreshLoadouts()
    {
        _loadoutWindow?.Close();
    }

    private void OpenLoadout(JobPrototype? jobProto, RoleLoadout roleLoadout, RoleLoadoutPrototype roleLoadoutProto)
    {
        _loadoutWindow?.Close();
        _loadoutWindow = null;

        var collection = IoCManager.Instance;
        if (collection is null || _playerManager.LocalSession is null || _profile is null)
            return;

        CreateLoadoutWindow(jobProto, roleLoadout, roleLoadoutProto, collection);
        if (_loadoutWindow is null)
            return;

        _loadoutWindow.RefreshLoadouts(roleLoadout, _playerManager.LocalSession, collection);
        _loadoutWindow.OpenCenteredLeft();
        OnJobOverride?.Invoke(jobProto);

        if (_profile is not null)
            UpdateJobPriorities();
    }

    private void CreateLoadoutWindow(JobPrototype? jobProto,
        RoleLoadout roleLoadout,
        RoleLoadoutPrototype roleLoadoutProto,
        IDependencyCollection collection)
    {
        var session = _playerManager.LocalSession;
        if (collection is null || session is null || _profile is null)
            return;

        _loadoutWindow = new LoadoutWindow(_profile,
            roleLoadout,
            roleLoadoutProto,
            session,
            collection)
        {
            Title = Loc.GetString("loadout-window-title-loadout", ("job", $"{jobProto?.LocalizedName}")),
        };

        _loadoutWindow.OnNameChanged += name =>
        {
            roleLoadout.EntityName = name;
            _profile = _profile.WithLoadout(roleLoadout);
            OnProfileUpdated?.Invoke(_profile);
        };

        _loadoutWindow.OnLoadoutPressed += (loadoutGroup, loadoutProto) =>
        {
            roleLoadout.AddLoadout(loadoutGroup, loadoutProto, _prototypeManager);
            _loadoutWindow.RefreshLoadouts(roleLoadout, session, collection);
            _profile = _profile?.WithLoadout(roleLoadout);
            OnJobUpdated?.Invoke(_profile);
        };

        _loadoutWindow.OnLoadoutUnpressed += (loadoutGroup, loadoutProto) =>
        {
            roleLoadout.RemoveLoadout(loadoutGroup, loadoutProto, _prototypeManager);
            _loadoutWindow.RefreshLoadouts(roleLoadout, session, collection);
            _profile = _profile?.WithLoadout(roleLoadout);
            OnJobUpdated?.Invoke(_profile);
        };

        _loadoutWindow.OnClose += () => { OnJobOverride?.Invoke(null); };
    }
}
