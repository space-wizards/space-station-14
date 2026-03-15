using Content.Client.UserInterface.Controls;
using Content.Shared.BloodCult;
using Robust.Client.GameObjects;
using Robust.Shared.Localization;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Numerics;
using Content.Shared.BloodCult.Components;
using Content.Shared.BloodCult.Prototypes;

namespace Content.Client.BloodCult.UI;
/// <summary>
/// Radial menu popup for selecting a spell to memorize.
/// When a player uses the ability to carve a spell into themselves, 
/// pops up a radial menu to choose which of 3 spells to carve. 
/// Same behavior with Empower runes, if they click the empower rune it
/// opens the same radial menu and allows them to choose which spell to add.
/// </summary>
public sealed partial class SpellRadialMenu : RadialMenu
{
    [Dependency] private readonly IEntitySystemManager _entitySystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] private readonly ILocalizationManager _loc = default!;
    private SpriteSystem _spriteSystem = default!;

	public event Action<ProtoId<CultAbilityPrototype>>? SendSpellsMessageAction;

    public EntityUid Entity { get; set; }

    public SpellRadialMenu()
    {
        RobustXamlLoader.Load(this);
    }

    public void InitializeDependencies(IDependencyCollection dependencies)
    {
        dependencies.InjectDependencies(this);
        _spriteSystem = _entitySystem.GetEntitySystem<SpriteSystem>();
    }

    public void SetEntity(EntityUid uid)
    {
        Entity = uid;
        RefreshUI();
    }

    private void RefreshUI()
    {
        var main = FindControl<RadialContainer>("Main");
        if (main == null)
            return;

        var player = _playerManager.LocalEntity;

        foreach (var cultAbility in CultistSpellComponent.ValidSpells)
		{
			_prototypeManager.TryIndex<CultAbilityPrototype>(cultAbility, out var cultAbilityPrototype);
			if (cultAbilityPrototype == null || cultAbilityPrototype.ActionPrototypes == null)
				continue;
			EntProtoId selectedProto = cultAbilityPrototype.ActionPrototypes[0];
			_prototypeManager.TryIndex<EntityPrototype>(selectedProto, out var spellPrototype);
			if (spellPrototype == null)
				continue;

			var tooltipKey = $"bloodcult-spell-tooltip-{cultAbility.Id}";
			var tooltip = _loc.TryGetString(tooltipKey, out var localized)
				? localized
				: spellPrototype.Name + ": " + spellPrototype.Description;

			var button = new SpellsMenuButton
            {
                StyleClasses = { "RadialMenuButton" },
                SetSize = new Vector2(64, 64),
                ToolTip = tooltip,
                ProtoId = cultAbility
            };
            var texture = new TextureRect
            {
                VerticalAlignment = VAlignment.Center,
                HorizontalAlignment = HAlignment.Center,
                Texture = _spriteSystem.GetPrototypeIcon(selectedProto).Default,
                TextureScale = new Vector2(1f, 1f)
            };

            button.AddChild(texture);
            main.AddChild(button);
        }

        AddSpellButtonOnClickAction(main);
    }

    private void AddSpellButtonOnClickAction(RadialContainer mainControl)
    {
        if (mainControl == null)
            return;

        foreach(var child in mainControl.Children)
        {
            var castChild = child as SpellsMenuButton;

            if (castChild == null)
                continue;

            castChild.OnButtonUp += _ =>
            {
                SendSpellsMessageAction?.Invoke(castChild.ProtoId);
                Close();
            };
        }
    }

    public sealed class SpellsMenuButton : RadialMenuButton
    {
		public required ProtoId<CultAbilityPrototype> ProtoId { get; set; }
    }
}
