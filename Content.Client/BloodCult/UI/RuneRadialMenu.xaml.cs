// SPDX-FileCopyrightText: 2025 Skye <57879983+Rainbeon@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 kbarkevich <24629810+kbarkevich@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
// SPDX-FileCopyrightText: 2026 Terkala <appleorange64@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using Content.Client.UserInterface.Controls;
//using Content.Shared.Heretic;
//using Content.Shared.Heretic.Prototypes;
using Content.Shared.BloodCult;
//using Content.Shared.BloodCult.Prototypes;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.GameObjects;
using Robust.Shared.Graphics;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;
using System.Numerics;
using Content.Shared.BloodCult.Components;

namespace Content.Client.BloodCult.UI;

public sealed partial class RuneRadialMenu : RadialMenu
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    private SpriteSystem _spriteSystem = default!;

	public event Action<string>? SendRunesMessageAction;

    public EntityUid Entity { get; set; }

    public RuneRadialMenu()
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

		// TODO: Switch this to using a list of available rituals gotten
		// from the BloodCultistComponent
        //if (!_entityManager.TryGetComponent<HereticComponent>(player, out var heretic))
        //    return;
        //foreach (var ritual in heretic.KnownRituals)

        foreach (var rune in BloodCultRuneCarverComponent.ValidRunes)//runes)
		{
            // Get the prototype name for the tooltip
            var tooltipText = rune;
            if (_prototypeManager.TryIndex<EntityPrototype>(rune, out var runePrototype))
            {
                tooltipText = runePrototype.Name;
            }

			if (rune != "TearVeilRune")
			{
				var button = new RunesMenuButton
				{
					StyleClasses = { "RadialMenuButton" },
					SetSize = new Vector2(64, 64),
					ToolTip = tooltipText,
					ProtoId = rune
				};

				var texture = new TextureRect
				{
					VerticalAlignment = VAlignment.Center,
					HorizontalAlignment = HAlignment.Center,
					Texture = GetRuneIconTexture(rune),
					TextureScale = new Vector2(2f, 2f)
				};
				button.AddChild(texture);
				main.AddChild(button);
			}
			else if (_entityManager.TryGetComponent<BloodCultistComponent>(player, out var bloodCultist) && bloodCultist.ShowTearVeilRune)
            {
				var button = new RunesMenuButton
				{
					StyleClasses = { "RadialMenuButton" },
					SetSize = new Vector2(96, 96),
					ToolTip = tooltipText,
					ProtoId = rune
				};

				var texture = new TextureRect
				{
					VerticalAlignment = VAlignment.Center,
					HorizontalAlignment = HAlignment.Center,
					Texture = GetRuneIconTexture(rune),
					TextureScale = new Vector2(1f, 1f)
				};
				button.AddChild(texture);
				main.AddChild(button);
			}
        }

        AddRuneButtonOnClickAction(main);
    }

    // All the runes have a -icon variant in the rsi, so we can just load the texture from the rsi
    // TearVeilRune is special because it has a different rsi
    private Texture GetRuneIconTexture(string rune)
    {
        var iconName = rune == "TearVeilRune" 
            ? "narsierune-icon" 
            : rune.Replace("Rune", "").ToLowerInvariant() + "-icon";
        
        var rsiPath = rune == "TearVeilRune"
            ? "Structures/BloodCult/narsierune.rsi"
            : "Structures/BloodCult/bloodrune.rsi";

        // Load the RSI state properly instead of raw PNG
        var fullRsiPath = SpriteSpecifierSerializer.TextureRoot / new ResPath(rsiPath);
        if (_resourceCache.TryGetResource<RSIResource>(fullRsiPath, out var rsiResource))
        {
            if (rsiResource.RSI.TryGetState(iconName, out var state))
            {
                return state.Frame0;
            }
        }

        // Fallback to prototype icon if RSI state not found
        return _spriteSystem.GetPrototypeIcon(rune).Default;
    }

    private void AddRuneButtonOnClickAction(RadialContainer mainControl)
    {
        if (mainControl == null)
            return;

        foreach(var child in mainControl.Children)
        {
            var castChild = child as RunesMenuButton;

            if (castChild == null)
                continue;

            castChild.OnButtonUp += _ =>
            {
                SendRunesMessageAction?.Invoke(castChild.ProtoId);
                Close();
            };
        }
    }

    public sealed class RunesMenuButton : RadialMenuButton
    {
		public required string ProtoId { get; set; }
    }
}
