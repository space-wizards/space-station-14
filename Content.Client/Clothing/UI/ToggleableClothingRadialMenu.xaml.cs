using Content.Client.UserInterface.Controls;
using Content.Shared.Clothing.Components;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using System.Numerics;

namespace Content.Client.Clothing.UI;

public sealed partial class ToggleableClothingRadialMenu : RadialMenu
{
    [Dependency] private readonly EntityManager _entityManager = default!;

    public event Action<EntityUid>? SendToggleClothingMessageAction;

    public EntityUid Entity { get; set; }

    public ToggleableClothingRadialMenu()
    {
        IoCManager.InjectDependencies(this);
        RobustXamlLoader.Load(this);
    }

    public void SetEntity(EntityUid uid)
    {
        Entity = uid;
        RefreshUI();
    }

    public void RefreshUI()
    {
        var main = FindControl<RadialContainer>("Main");

        if (!_entityManager.TryGetComponent<ToggleableClothingComponent>(Entity, out var clothing))
            return;

        var clothingContainer = clothing.Container;

        if (clothingContainer == null)
            return;

        foreach(var attached in clothing.ClothingUids)
        {
            // Change tooltip text if attached clothing is toggle/untoggled
            var tooltipText = Loc.GetString("toggleable-clothing-unattach-tooltip");

            if (clothingContainer.Contains(attached.Key))
                tooltipText = Loc.GetString("toggleable-clothing-attach-tooltip");

            var button = new ToggleableClothingRadialMenuButton()
            {
                StyleClasses = { "RadialMenuButton" },
                SetSize = new Vector2(64, 64),
                ToolTip = tooltipText,
                AttachedClothingId = attached.Key
            };

            var entProtoView = new EntityPrototypeView()
            {
                SetSize = new Vector2(48, 48),
                VerticalAlignment = VAlignment.Center,
                HorizontalAlignment = HAlignment.Center,
                Stretch = SpriteView.StretchMode.Fill
            };

            entProtoView.SetEntity(attached.Key);

            button.AddChild(entProtoView);
            main.AddChild(button);
        }

        AddToggleableClothingMenuButtonOnClickAction(main);
    }

    private void AddToggleableClothingMenuButtonOnClickAction(Control control)
    {
        var mainControl = control as RadialContainer;

        if (mainControl == null)
            return;

        foreach (var child in mainControl.Children)
        {
            var castChild = child as ToggleableClothingRadialMenuButton;

            if (castChild == null)
                return;

            castChild.OnButtonDown += _ =>
            {
                SendToggleClothingMessageAction?.Invoke(castChild.AttachedClothingId);
                mainControl.DisposeAllChildren();
                RefreshUI();
            };
        }
    }
}

public sealed class ToggleableClothingRadialMenuButton : RadialMenuTextureButton
{
    public EntityUid AttachedClothingId { get; set; }
}
