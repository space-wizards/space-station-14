using Content.Shared.Botany.PlantAnalyzer;
using JetBrains.Annotations;
using Robust.Client.GameObjects;


namespace Content.Client.Plant_Analyzer.UI;

[UsedImplicitly]
public sealed class PlantAnalyzerBoundUserInterface : BoundUserInterface
{
    private PlantAnalyzerWindow? _window;

    public PlantAnalyzerBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = new PlantAnalyzerWindow
        {
            Title = IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(Owner.Owner).EntityName,
        };
        _window.OnClose += Close;
        _window.OpenToLeft();
    }

   protected override void UpdateState(BoundUserInterfaceState state)
   {
      base.UpdateState(state);

       if (_window == null)
          return;

      if (state is not PlantAnalyzerScannedSeedPlantInformation cast)
         return;



      _window.UpdateState(cast);
   }





    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        if (_window != null)
            _window.OnClose -= Close;

        _window?.Dispose();
    }

}
