using System.Linq;
using System.Numerics;
using Content.Client.Alerts;
using Content.Client.Hands.Systems;
using Content.Client.Humanoid;
using Content.Client.Inventory;
using Content.Client.Preferences;
using Content.Client.UserInterface.Controls;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Inventory;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Content.Shared.Clothing.Components;
using Content.Shared.Loadout;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Lobby.UI
{
    public sealed class LobbyCharacterPreviewPanel : Control
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IClientPreferencesManager _preferencesManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;


        private EntityUid? _previewDummy;
        private readonly Label _summaryLabel;
        private readonly BoxContainer _loaded;
        private readonly BoxContainer _viewBox;
        private readonly Label _unloaded;

        public LobbyCharacterPreviewPanel()
        {
            IoCManager.InjectDependencies(this);
            var header = new NanoHeading
            {
                Text = Loc.GetString("lobby-character-preview-panel-header")
            };

            CharacterSetupButton = new Button
            {
                Text = Loc.GetString("lobby-character-preview-panel-character-setup-button"),
                HorizontalAlignment = HAlignment.Center,
                Margin = new Thickness(0, 5, 0, 0),
            };

            _summaryLabel = new Label
            {
                HorizontalAlignment = HAlignment.Center,
                Margin = new Thickness(3, 3),
            };

            var vBox = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical
            };
            _unloaded = new Label { Text = Loc.GetString("lobby-character-preview-panel-unloaded-preferences-label") };

            _loaded = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                Visible = false
            };
            _viewBox = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                HorizontalAlignment = HAlignment.Center,
            };
            var _vSpacer = new VSpacer();

            _loaded.AddChild(_summaryLabel);
            _loaded.AddChild(_viewBox);
            _loaded.AddChild(_vSpacer);
            _loaded.AddChild(CharacterSetupButton);

            vBox.AddChild(header);
            vBox.AddChild(_loaded);
            vBox.AddChild(_unloaded);
            AddChild(vBox);

            UpdateUI();
        }

        public Button CharacterSetupButton { get; }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;
            if (_previewDummy != null) _entityManager.DeleteEntity(_previewDummy.Value);
            _previewDummy = default;
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
                if (_preferencesManager.Preferences?.SelectedCharacter is not HumanoidCharacterProfile selectedCharacter)
                {
                    _summaryLabel.Text = string.Empty;
                }
                else
                {
                    _previewDummy = _entityManager.SpawnEntity(_prototypeManager.Index<SpeciesPrototype>(selectedCharacter.Species).DollPrototype, MapCoordinates.Nullspace);
                    _viewBox.DisposeAllChildren();
                    var spriteView = new SpriteView
                    {
                        OverrideDirection = Direction.South,
                        Scale = new Vector2(4f, 4f),
                        MaxSize = new Vector2(112, 112),
                        Stretch = SpriteView.StretchMode.Fill,
                    };
                    spriteView.SetEntity(_previewDummy.Value);
                    _viewBox.AddChild(spriteView);
                    _summaryLabel.Text = selectedCharacter.Summary;
                    _entityManager.System<HumanoidAppearanceSystem>().LoadProfile(_previewDummy.Value, selectedCharacter);
                    GiveDummyJobClothes(_previewDummy.Value, selectedCharacter);
                    GiveDummyLoadoutItems(_previewDummy.Value, selectedCharacter);
                }
            }
        }

        public static void GiveDummyJobClothes(EntityUid dummy, HumanoidCharacterProfile profile)
        {
            var protoMan = IoCManager.Resolve<IPrototypeManager>();
            var entMan = IoCManager.Resolve<IEntityManager>();
            var invSystem = EntitySystem.Get<ClientInventorySystem>();

            var highPriorityJob = profile.JobPriorities.FirstOrDefault(p => p.Value == JobPriority.High).Key;

            // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract (what is resharper smoking?)
            var job = protoMan.Index<JobPrototype>(highPriorityJob ?? SharedGameTicker.FallbackOverflowJob);

            if (job.StartingGear != null && invSystem.TryGetSlots(dummy, out var slots))
            {
                var gear = protoMan.Index<StartingGearPrototype>(job.StartingGear);

                foreach (var slot in slots)
                {
                    var itemType = gear.GetGear(slot.Name, profile);
                    if (invSystem.TryUnequip(dummy, slot.Name, out var unequippedItem, true, true))
                    {
                        entMan.DeleteEntity(unequippedItem.Value);
                    }

                    if (itemType != string.Empty)
                    {
                        var item = entMan.SpawnEntity(itemType, MapCoordinates.Nullspace);
                        invSystem.TryEquip(dummy, item, slot.Name, true, true);
                    }
                }
            }
        }
		
		public static void GiveDummyLoadoutItems(EntityUid dummy, HumanoidCharacterProfile profile)
        {
            var protoMan = IoCManager.Resolve<IPrototypeManager>();
            var entMan = IoCManager.Resolve<IEntityManager>();
            var handsSystem = entMan.System<HandsSystem>();
            var invSystem = entMan.System<ClientInventorySystem>();

            var highPriorityJobId = profile.JobPriorities.FirstOrDefault(p => p.Value == JobPriority.High).Key;

            foreach (var loadoutId in profile.LoadoutPreferences)
            {

                if (!(highPriorityJobId is null) && (!protoMan.TryIndex<JobPrototype>(highPriorityJobId, out var job) || !job.DoLoadout))
					return;
				
				if (!protoMan.TryIndex<LoadoutPrototype>(loadoutId, out var loadout))
                    continue;

                var isWhitelisted = loadout.WhitelistJobs != null &&
                                    !loadout.WhitelistJobs.Contains(highPriorityJobId ?? "");
                var isBlacklisted = highPriorityJobId != null &&
                                    loadout.BlacklistJobs != null &&
                                    loadout.BlacklistJobs.Contains(highPriorityJobId);
                var isSpeciesRestricted = loadout.SpeciesRestrictions != null &&
                                          loadout.SpeciesRestrictions.Contains(profile.Species);

                if (isWhitelisted || isBlacklisted || isSpeciesRestricted)
                    continue;

                var entity = entMan.SpawnEntity(loadout.Prototype, MapCoordinates.Nullspace);

                // Take in hand if not clothes
                if (!entMan.TryGetComponent<ClothingComponent>(entity, out var clothing))
                {
                    handsSystem.TryPickup(dummy, entity);
                    continue;
                }

                // Automatically search empty slot for clothes to equip
                string? firstSlotName = null;
                var isEquipped = false;
				if (invSystem.TryGetSlots(dummy, out var slotDefinitions))
				{
					foreach (var slot in slotDefinitions)
					{
						if (!clothing.Slots.HasFlag(slot.SlotFlags))
							continue;

						firstSlotName ??= slot.Name;

						if (invSystem.TryGetSlotEntity(dummy, slot.Name, out var _))
							continue;

						if (loadout.Exclusive && invSystem.TryUnequip(dummy, firstSlotName, out var removedItem, true, true))
							entMan.DeleteEntity(removedItem.Value);

						if (!invSystem.TryEquip(dummy, entity, slot.Name, true, true))
							continue;

						isEquipped = true;
						break;
					}
				}

                if (isEquipped || firstSlotName == null)
                    continue;

                // Force equip to first valid clothes slot even there already item
                if (invSystem.TryUnequip(dummy, firstSlotName, out var unequippedItem, true, true))
                    entMan.DeleteEntity(unequippedItem.Value);
                invSystem.TryEquip(dummy, entity, firstSlotName, true, true);
            }
        }
    }
}
