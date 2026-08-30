namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private void UpdateMarkings()
    {
        if (Profile == null)
        {
            return;
        }

        _markingsModel.OrganData = _markingManager.GetMarkingData(Profile.Species);
        _markingsModel.OrganProfileData = _markingManager.GetProfileData(Profile.Species, Profile.Sex, Profile.Appearance.SkinColor, Profile.Appearance.EyeColor);
        _markingsModel.Markings = Profile.Appearance.Markings;
    }

    private void OnMarkingChange()
    {
        if (Profile is null)
            return;

        Profile = Profile.WithCharacterAppearance(Profile.Appearance.WithMarkings(_markingsModel.Markings));
        ReloadProfilePreview();
        SetDirty();
    }
}
