using Content.Client._Starlight.Character.Info.UI;
using Content.Shared._Starlight.Character.Info;

// ReSharper disable CheckNamespace
namespace Content.Client.UserInterface.Systems.Character;

public sealed partial class CharacterUIController
{
    private Dictionary<EntityUid, CharacterInspectWindow> _openInspectionWindows = new();


    public void OpenInspectCharacterWindow(EntityUid target, EntityUid viewer)
    {
        if (!target.Valid)
            return;

        if (target == viewer)
        {
            //If attempting to inspect own character, redirect to character window
            if (_window == null || _window.IsOpen)
            {
                return;
            }

            _characterInfo.RequestCharacterInfo();
            ALSetSelfCharacterInfo();
            _window.Open();
            return;
        }

        if (_openInspectionWindows.TryGetValue(target, out var window))
        {
            window.OpenCentered();
            return;
        }

        window = new CharacterInspectWindow();
        window.SetCharacter(target, EntityManager, viewer.Valid ? viewer : target);

        _openInspectionWindows[target] = window;

        window.OnClose += () => _openInspectionWindows.Remove(target);
        window.Title = Loc.GetString("character-info-window-title", ("player", target));
    }

    private void ALClearSelfCharacterInfo()
    {
        if (_window == null)
            return;
        _window.InfoIC.ClearCharacter();
        _window.InfoOOC.ClearCharacter();
    }

    private void ALSetSelfCharacterInfo()
    {
        if (_window == null)
            return;
        var ent = _window.CharacterInfo.CharacterPreview.CharacterSpriteView.Entity;
        if (!ent.HasValue)
        {
            _window.InfoIC.ClearCharacter();
            _window.InfoOOC.ClearCharacter();
            return;
        }

        _window.InfoIC.SetCharacter(ent, EntityManager, ent);
        _window.InfoOOC.SetCharacter(ent, EntityManager, ent);
    }
}