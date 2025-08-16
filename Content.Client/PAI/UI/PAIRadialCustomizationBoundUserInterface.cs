using Content.Client.UserInterface.Controls;
using Content.Shared.PAI;
using Robust.Shared.Utility;
using Robust.Client.UserInterface;

namespace Content.Client.PAI;

public sealed class PAIRadialCustomizationBoundUserInterface : BoundUserInterface
{
    private SimpleRadialMenu? _menu;

    private readonly Dictionary<PAIEmotion, (string Tooltip, SpriteSpecifier Sprite)> _emotionInfo = new()
    {
        [PAIEmotion.Neutral] = ("pai-emotion-neutral", new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/Radial/PAI/neutral.png"))),
        [PAIEmotion.Happy] = ("pai-emotion-happy", new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/Radial/PAI/smile.png"))),
        [PAIEmotion.Sad] = ("pai-emotion-sad", new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/Emotes/cry.png"))),
        [PAIEmotion.Angry] = ("pai-emotion-angry", new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/Radial/PAI/angry.png")))
    };

    public PAIRadialCustomizationBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        if (!EntMan.TryGetComponent<PAICustomizationComponent>(Owner, out var customization))
            return;

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.Track(Owner);

        var models = ConvertToButtons(customization.CurrentEmotion);
        _menu.SetButtons(models);

        _menu.OpenOverMouseScreenPosition();
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);

        if (message is PAIEmotionStateMessage stateMessage)
        {
            if (_menu != null && _menu.IsOpen)
            {
                _menu.Close();
                Open();
            }
        }
    }

    private IEnumerable<RadialMenuOption> ConvertToButtons(PAIEmotion currentEmotion)
    {
        var options = new List<RadialMenuOption>();

        foreach (var (emotion, info) in _emotionInfo)
        {
            var option = new RadialMenuActionOption<PAIEmotion>(HandleEmotionClick, emotion)
            {
                Sprite = info.Sprite,
                ToolTip = Loc.GetString(info.Tooltip)
            };

            options.Add(option);
        }

        return options;
    }

    private void HandleEmotionClick(PAIEmotion emotion)
    {
        SendMessage(new PAIEmotionMessage(emotion));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        _menu?.Dispose();
    }
}
