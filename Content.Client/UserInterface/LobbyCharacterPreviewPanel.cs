using System.Linq;
using Content.Client.GameObjects.Components.HUD.Inventory;
using Content.Client.GameObjects.Components.Mobs;
using Content.Client.Interfaces;
using Content.Shared;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using static Content.Shared.GameObjects.Components.Inventory.EquipmentSlotDefines;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.UserInterface
{
    public class LobbyCharacterPreviewPanel : Control
    {
        private readonly IClientPreferencesManager _preferencesManager;
        private IEntity _previewDummy;
        private readonly Label _summaryLabel;
        private readonly VBoxContainer _loaded;
        private readonly Label _unloaded;

        public LobbyCharacterPreviewPanel(IEntityManager entityManager,
            IClientPreferencesManager preferencesManager)
        {
            _preferencesManager = preferencesManager;
            _previewDummy = entityManager.SpawnEntity("HumanMob_Dummy", MapCoordinates.Nullspace);

            var header = new NanoHeading
            {
                Text = Loc.GetString("Character")
            };

            CharacterSetupButton = new Button
            {
                Text = Loc.GetString("Customize"),
                SizeFlagsHorizontal = SizeFlags.None
            };

            _summaryLabel = new Label();

            var viewSouth = MakeSpriteView(_previewDummy, Direction.South);
            var viewNorth = MakeSpriteView(_previewDummy, Direction.North);
            var viewWest = MakeSpriteView(_previewDummy, Direction.West);
            var viewEast = MakeSpriteView(_previewDummy, Direction.East);

            var vBox = new VBoxContainer();

            vBox.AddChild(header);

            _unloaded = new Label {Text = "Your character preferences have not yet loaded, please stand by."};

            _loaded = new VBoxContainer {Visible = false};

            _loaded.AddChild(CharacterSetupButton);
            _loaded.AddChild(_summaryLabel);

            var hBox = new HBoxContainer();
            hBox.AddChild(viewSouth);
            hBox.AddChild(viewNorth);
            hBox.AddChild(viewWest);
            hBox.AddChild(viewEast);

            _loaded.AddChild(hBox);

            vBox.AddChild(_loaded);
            vBox.AddChild(_unloaded);
            AddChild(vBox);

            UpdateUI();

            _preferencesManager.OnServerDataLoaded += UpdateUI;
        }

        public Button CharacterSetupButton { get; }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _preferencesManager.OnServerDataLoaded -= UpdateUI;

            if (!disposing) return;
            _previewDummy.Delete();
            _previewDummy = null;
        }

        private static SpriteView MakeSpriteView(IEntity entity, Direction direction)
        {
            return new()
            {
                Sprite = entity.GetComponent<ISpriteComponent>(),
                OverrideDirection = direction,
                Scale = (2, 2)
            };
        }

        public void UpdateUI()
        {
            if (!_preferencesManager.ServerDataLoaded)
            {
                _loaded.Visible = false;
                _unloaded.Visible = true;
            }
            else
            {
                _loaded.Visible = true;
                _unloaded.Visible = false;
                if (_preferencesManager.Preferences.SelectedCharacter is not HumanoidCharacterProfile selectedCharacter)
                {
                    _summaryLabel.Text = string.Empty;
                }
                else
                {
                    _summaryLabel.Text = selectedCharacter.Summary;
                    var component = _previewDummy.GetComponent<HumanoidAppearanceComponent>();
                    component.UpdateFromProfile(selectedCharacter);

                    GiveDummyJobClothes(_previewDummy, selectedCharacter);
                }
            }
        }

        public static void GiveDummyJobClothes(IEntity dummy, HumanoidCharacterProfile profile)
        {
            var protoMan = IoCManager.Resolve<IPrototypeManager>();
            var entityMan = IoCManager.Resolve<IEntityManager>();

            var inventory = dummy.GetComponent<ClientInventoryComponent>();

            var highPriorityJob = profile.JobPriorities.FirstOrDefault(p => p.Value == JobPriority.High).Key;

            var job = protoMan.Index<JobPrototype>(highPriorityJob ?? SharedGameTicker.OverflowJob);
            var gear = protoMan.Index<StartingGearPrototype>(job.StartingGear);

            inventory.ClearAllSlotVisuals();

            foreach (var slot in AllSlots)
            {
                var itemType = gear.GetGear(slot, profile);
                if (itemType != "")
                {
                    var item = entityMan.SpawnEntity(itemType, MapCoordinates.Nullspace);
                    inventory.SetSlotVisuals(slot, item);
                    item.Delete();
                }
            }
        }
    }
}
