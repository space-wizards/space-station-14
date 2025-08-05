using System.Numerics;
using Content.Client.Lobby;
using Content.Client.Lobby.UI;
using Content.Client.Players.PlayTimeTracking;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client._Starlight.NewLife;

public sealed partial class NewLifeWindow : DefaultWindow
{
    private readonly IClientPreferencesManager _preferencesManager = default!;
    [Dependency] private readonly JobRequirementsManager _jobRequirements = default!;

    private readonly Dictionary<NetEntity, Dictionary<string, List<JobButton>>> _jobButtons = new();
    private readonly Dictionary<NetEntity, Dictionary<string, BoxContainer>> _jobCategories = new();
    private HashSet<int> _usedSlots = [];
    private int _remainingLives = 5;
    private int _maxLives = 5;

    public readonly LateJoinGui LateJoinGui = default!;
    
    public NewLifeWindow(IClientPreferencesManager preferencesManager)
    {
        _preferencesManager = preferencesManager;
        
        SetSize = new Vector2(685, 560);
        MinSize = new Vector2(685, 560);
        LateJoinGui = new LateJoinGui();
        IoCManager.InjectDependencies(this);
        UpdateTitle();

        LateJoinGui.Contents.Orphan();
        LateJoinGui.Contents.Margin = new Thickness(0, 25, 0, 0);
        LateJoinGui.SelectedId += (_) => Close();
        AddChild(LateJoinGui.Contents);
        _jobRequirements.Updated += RemoveUsedCharacters;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _jobRequirements.Updated -= RemoveUsedCharacters;
            _jobButtons.Clear();
            _jobCategories.Clear();
        }
    }

    private void RemoveUsedCharacters()
    {
        if (_preferencesManager.Preferences is null)
            return;
        foreach (var control in LateJoinGui.CharList.Children)
        {
            if (control is not CharacterPickerButton pickerButton)
                continue;

            pickerButton.Disabled = _usedSlots.Contains(_preferencesManager.Preferences.IndexOfCharacter(pickerButton.Profile));
            //if we have no lives left, disable all buttons
            if (_remainingLives <= 0)
            {
                pickerButton.Disabled = true;
            }

            if (pickerButton is { Disabled: true, Pressed: true })
            {
                pickerButton.Pressed = false;
            }
        }
    }

    private void UpdateTitle()
    {
        Title = Loc.GetString("ghost-new-life-window-title", ("remainingLives", _remainingLives), ("maxLives", _maxLives));
    }

    public void ReloadUI(HashSet<int> usedSlots, int remainingLives, int maxLives)
    {
        _usedSlots = usedSlots;
        _remainingLives = remainingLives;
        _maxLives = maxLives;
        RemoveUsedCharacters();
        UpdateTitle();
    }
}
