using Content.Client.PAI.UI;
using Content.Shared.PAI;

namespace Content.Client.PAI;

public sealed class PAICustomizationBoundUserInterface : BoundUserInterface
{
    private PAICustomizationMenu? _menu;

    public PAICustomizationBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        // Retrieve the current emotion from the PAICustomizationComponent
        var currentEmotion = PAIEmotion.Neutral;
        if (EntMan.TryGetComponent<PAICustomizationComponent>(Owner, out var emotionComp))
            currentEmotion = emotionComp.CurrentEmotion;

        // Pass both current emotion and the PAI entity for preview
        _menu = new PAICustomizationMenu(currentEmotion, Owner);
        _menu.OpenCentered();
        _menu.OnClose += Close;
        _menu.OnEmotionSelected += OnEmotionSelected;
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);

        if (message is PAIEmotionStateMessage stateMessage)
        {
            UpdateEmotion(stateMessage.Emotion);
        }
    }

    private void OnEmotionSelected(PAIEmotion emotion)
    {
        SendMessage(new PAIEmotionMessage(emotion));
    }

    public void UpdateEmotion(PAIEmotion emotion)
    {
        _menu?.UpdateEmotion(emotion);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        _menu?.Dispose();
    }
}
