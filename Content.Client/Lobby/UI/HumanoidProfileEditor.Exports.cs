using System.IO;
using Content.Client.Sprite;
using Content.Shared.Preferences;
using Robust.Client.UserInterface;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private bool _exporting;
    private bool _imaging;

    private async void ExportImage()
    {
        if (_imaging)
            return;

        var dir = SpriteView.OverrideDirection ?? Direction.South;

        // I tried disabling the button but it looks sorta goofy as it only takes a frame or two to save
        _imaging = true;
        await _entManager.System<ContentSpriteSystem>().Export(SpriteView.PreviewDummy, dir, includeId: false);
        _imaging = false;
    }

    private async void ImportProfile()
    {
        if (_exporting || CharacterSlot == null || Profile == null)
            return;

        StartExport();
        await using var file = await _dialogManager.OpenFile(new FileDialogFilters(new FileDialogFilters.Group("yml")), FileAccess.Read);

        if (file == null)
        {
            EndExport();
            return;
        }

        try
        {
            var profile = HumanoidCharacterProfile.FromStream(file, _playerManager.LocalSession!);
            var oldProfile = Profile;
            SetProfile(profile, CharacterSlot);

            IsDirty = !profile.MemberwiseEquals(oldProfile);
        }
        catch (Exception exc)
        {
            _sawmill.Error($"Error when importing profile\n{exc.StackTrace}");
        }
        finally
        {
            EndExport();
        }
    }

    private async void ExportProfile()
    {
        if (Profile == null || _exporting)
            return;

        StartExport();
        var file = await _dialogManager.SaveFile(new FileDialogFilters(new FileDialogFilters.Group("yml")));

        if (file == null)
        {
            EndExport();
            return;
        }

        try
        {
            var dataNode = Profile.ToDataNode();
            await using var writer = new StreamWriter(file.Value.fileStream);
            dataNode.Write(writer);
        }
        catch (Exception exc)
        {
            _sawmill.Error($"Error when exporting profile\n{exc.StackTrace}");
        }
        finally
        {
            EndExport();
            await file.Value.fileStream.DisposeAsync();
        }
    }

    private void StartExport()
    {
        _exporting = true;
        ImportButton.Disabled = true;
        ExportButton.Disabled = true;
    }

    private void EndExport()
    {
        _exporting = false;
        ImportButton.Disabled = false;
        ExportButton.Disabled = false;
    }
}
