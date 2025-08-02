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

        // Retrieve the current emotion and glasses from the PAICustomizationComponent
        var currentEmotion = PAIEmotion.Neutral;
        var currentGlasses = PAIGlasses.None;

        if (EntMan.TryGetComponent<PAICustomizationComponent>(Owner, out var customizationComp))
        {
            currentEmotion = customizationComp.CurrentEmotion;
            currentGlasses = customizationComp.CurrentGlasses;
        }

        // Pass both current emotion, glasses and the PAI entity for preview
        _menu = new PAICustomizationMenu(currentEmotion, currentGlasses, Owner);
        _menu.OpenCentered();
        _menu.OnClose += Close;
        _menu.OnEmotionSelected += OnEmotionSelected;
        _menu.OnGlassesSelected += OnGlassesSelected;
        _menu.OnNameChanged += OnNameChanged;
        _menu.OnNameReset += OnNameReset;
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);

        switch (message)
        {
            case PAIEmotionStateMessage stateMessage:
                UpdateEmotion(stateMessage.Emotion);
                break;
            case PAIGlassesStateMessage glassesMessage:
                UpdateGlasses(glassesMessage.Glasses);
                break;
            case PAINameStateMessage nameMessage:
                UpdateName(nameMessage.Name);
                break;
        }
    }

    private void OnEmotionSelected(PAIEmotion emotion)
    {
        SendMessage(new PAIEmotionMessage(emotion));
    }

    private void OnGlassesSelected(PAIGlasses glasses)
    {
        SendMessage(new PAIGlassesMessage(glasses));
    }

    private void OnNameChanged(string name)
    {
        SendMessage(new PAISetNameMessage(name));
    }

    private void OnNameReset()
    {
        SendMessage(new PAIResetNameMessage());
    }

    public void UpdateEmotion(PAIEmotion emotion)
    {
        _menu?.UpdateEmotion(emotion);
    }

    public void UpdateGlasses(PAIGlasses glasses)
    {
        _menu?.UpdateGlasses(glasses);
    }

    public void UpdateName(string name)
    {
        _menu?.UpdateName(name);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        _menu?.Dispose();
    }
}
