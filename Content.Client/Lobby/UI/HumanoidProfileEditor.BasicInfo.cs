
using Content.Shared.Preferences;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private void SetName(string newName)
    {
        Profile = Profile?.WithName(newName);
        SetDirty();

        if (!IsDirty)
            return;

        SpriteView.SetName(newName);
    }

    private void UpdateNameEdit()
    {
        NameEdit.Text = Profile?.Name ?? "";
    }

    /// <summary>
    /// Randomize values selectively while respecting locked values.
    /// </summary>
    private void RandomizeProfile()
    {
        Profile = Profile == null
            ? HumanoidCharacterProfile.Random()
            : HumanoidCharacterProfile.Random(RandomizeLockButton.RandomizeCfg, Profile!);
        SetProfile(Profile, CharacterSlot);
        SetDirty();
    }
}
