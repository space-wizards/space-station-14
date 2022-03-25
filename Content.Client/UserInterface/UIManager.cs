namespace Content.Client.UserInterface;

[Virtual]
public abstract class UIController
{

}

//notices your IUI, UwU What's this?
public interface IUIControllerManager
{
    public void Initialize();
}


public sealed class UIControllerManager: IUIControllerManager
{
    public UIControllerManager()
    {

    }

    public void Initialize()
    {
    }
}
