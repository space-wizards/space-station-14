using Content.Shared.Preferences;
using Robust.Shared.Prototypes;

namespace Content.Client.Preferences.UI
{
    public sealed partial class HumanoidProfileEditor
    {
        private void RandomizeEverything()
        {
            Profile = HumanoidCharacterProfile.Random();
            UpdateControls();
            IsDirty = true;
        }

        private void RandomizeName()
        {
            if (Profile == null) return;
            var name = HumanoidCharacterProfile.GetName(Profile.Species, Profile.Gender);
            SetName(name);
            UpdateNameEdit();
        }
    }
}
