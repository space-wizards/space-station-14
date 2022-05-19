using Content.Shared.CharacterAppearance;
using Content.Shared.Dataset;
using Content.Shared.Preferences;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Client.Preferences.UI
{
    public sealed partial class HumanoidProfileEditor
    {
        private readonly IRobustRandom _random;
        private readonly IPrototypeManager _prototypeManager;

        private void RandomizeEverything()
        {
            Profile = HumanoidCharacterProfile.Random();
            UpdateSexControls();
            UpdateGenderControls();
            UpdateClothingControls();
            UpdateAgeEdit();
            UpdateNameEdit();
            UpdateHairPickers();
            UpdateEyePickers();

            _skinColor.Value = _random.Next(0, 100);
        }

        private void RandomizeName()
        {
            if (Profile == null) return;
            var name = Profile.Sex.GetName(Profile.Species, _prototypeManager, _random);
            SetName(name);
            UpdateNameEdit();
        }
    }
}
