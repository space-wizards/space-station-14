using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.Remoting;
using Content.Client.Eui;
using Content.Shared.Eui;
using Content.Shared.VotingNew;
using Robust.Client.Input;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.VotingNew.UI;

public sealed class VoteCallNewEui : BaseEui
{
    private readonly VoteCallNewMenu _menu;
    private readonly PresetControl _presetButtons = new();

    private Dictionary<string, string> _presets = new();
    private Dictionary<string, string> _presetsTypes = new();
    private Dictionary<Button, List<string>> _gameRulesPresets = new();

    private readonly Dictionary<int, CreateGameRulesPreset> _availableGameRulesPresets = new()
    {
    };

    public VoteCallNewEui()
    {
        _menu = new VoteCallNewMenu();
        _menu.VoteStartButton.OnPressed += VoteStartPressed;
        _menu.PresetsButton.OnItemSelected += PickGameRulesPreset;
    }

    private void VoteStartPressed(BaseButton.ButtonEventArgs obj)
    {
        var targetListButton =
            _presetButtons.ButtonsList
                .Where(x => x.Value.Pressed)
                .Select(x => x.Key);

        var targetList =
            _presets
                .Where(x => targetListButton.Contains(x.Value))
                .Select(x => x.Key)
                .ToList();

        SendMessage(new VoteCallNewEuiMsg.DoVote
        {
            TargetPresetList = targetList,
        });
    }

    private void SetPresetsList(List<string> presets)
    {
        _presetButtons.Populate(presets);
        _menu.PresetsContainer.AddChild(_presetButtons);
    }

    private void SetGameRulesPresets()
    {
        var rdmPresets =
            _presetsTypes
                .Where(x => x.Value == "rdm")
                .Select(x => x.Key)
                .ToList();

        _availableGameRulesPresets.Add(0, new CreateGameRulesPreset("РДМ", rdmPresets));
        _menu.PresetsButton.AddItem(_availableGameRulesPresets[0].Name, 0);

        var calmPresets =
            _presetsTypes
                .Where(x => x.Value == "calm")
                .Select(x => x.Key)
                .ToList();

        _availableGameRulesPresets.Add(1, new CreateGameRulesPreset("Спокойный", calmPresets));
        _menu.PresetsButton.AddItem(_availableGameRulesPresets[1].Name, 1);
    }

    private void PickGameRulesPreset(OptionButton.ItemSelectedEventArgs obj)
    {
        _menu.PresetsButton.SelectId(obj.Id);

        var presets = _availableGameRulesPresets[obj.Id].GameRulesPresets;
        foreach (var buttonPreset in _presetButtons.ButtonsList.Values)
        {
            buttonPreset.Pressed = presets.Any(x => _presets[x] == buttonPreset.Text);
        }
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not VoteCallNewEuiState s)
        {
            return;
        }

        _presets = s.Presets;
        _presetsTypes = s.PresetsTypes;
        SetPresetsList(s.Presets.Select(x => x.Value).ToList());
        SetGameRulesPresets();
    }

    public override void Opened()
    {
        _menu.OpenCentered();
    }

    public override void Closed()
    {
        _menu.Close();
    }
}

public record struct CreateGameRulesPreset
{
    public string Name;
    public List<string> GameRulesPresets;

    public CreateGameRulesPreset(string name, List<string> gameRulesPresets)
    {
        Name = name;
        GameRulesPresets = gameRulesPresets;
    }
}
