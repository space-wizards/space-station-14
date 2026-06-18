
using Content.Shared.Humanoid;
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

    private void RandomizeEverything()
    {
        Profile = HumanoidCharacterProfile.Random();
        SetProfile(Profile, CharacterSlot);
        SetDirty();
    }

    /// <summary>
    ///     Randomizes only the appearance of the character, without touching species, name, etc.
    /// </summary>
    private void RandomizeAppearance()
    {
        if (Profile == null)
        {
            return;
        }

        var appearance = HumanoidCharacterAppearance.Random(Profile.Species, Profile.Sex);

        Profile = Profile.WithCharacterAppearance(appearance);
        SetProfile(Profile, CharacterSlot);
        SetDirty();
    }

    private void RandomizeName()
    {
        if (Profile == null) return;
        var name = HumanoidCharacterProfile.GetName(Profile.Species, Profile.Gender);
        SetName(name);
        UpdateNameEdit();
    }
}
