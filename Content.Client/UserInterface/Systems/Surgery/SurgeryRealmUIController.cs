using System.IO;
using System.Threading.Tasks;
using Content.Client.Audio;
using Content.Client.Instruments;
using Content.Shared.Medical.Surgery;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.ContentPack;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Systems.Surgery;

public sealed class SurgeryRealmUIController : UIController
{
    [UISystemDependency] private readonly BackgroundAudioSystem? _backgroundAudio = default!;

    private SelfRequestWindow _selfRequestWindow = default!;
    private SurgeryRealmOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new SurgeryRealmOverlay(EntityManager, IoCManager.Resolve<IEyeManager>(),
            IoCManager.Resolve<IResourceCache>());
        IoCManager.Resolve<IOverlayManager>().AddOverlay(_overlay);

        SubscribeNetworkEvent<SurgeryRealmRequestSelfEvent>(OnSurgeryRequestSelf);

        // pjb dont look
        SubscribeNetworkEvent<SurgeryRealmStartEvent>((msg, args) => _ = OnSurgeryRealmStart(msg, args));
    }

    private void OnSurgeryRequestSelf(SurgeryRealmRequestSelfEvent msg, EntitySessionEventArgs args)
    {
        _selfRequestWindow?.Dispose();
        _selfRequestWindow = new SelfRequestWindow();
        _selfRequestWindow.OpenCentered();
        _selfRequestWindow.AcceptButton.OnPressed += buttonArgs =>
        {
            _selfRequestWindow.Dispose();
            IoCManager.Resolve<IEntityNetworkManager>().SendSystemNetworkMessage(new SurgeryRealmAcceptSelfEvent());
        };
    }

    private async Task OnSurgeryRealmStart(SurgeryRealmStartEvent msg, EntitySessionEventArgs args)
    {
        for (var i = 500; i < 10000; i += 500)
        {
            // I LOVE ambience code
            Timer.Spawn(i, () => _backgroundAudio?.EndAmbience());
        }

        var file = IoCManager.Resolve<IResourceManager>()
            .ContentFileRead(new ResourcePath("/Audio/Surgery/midilovania.mid"));
        await using var memStream = new MemoryStream((int) file.Length);
        // 100ms delay is due to a race condition or something idk.
        // While we're waiting, load it into memory.
        await Task.WhenAll(Timer.Delay(100), file.CopyToAsync(memStream));

        EntitySystem.Get<InstrumentSystem>()
            .OpenMidi(msg.Camera, memStream.GetBuffer().AsSpan(0, (int)memStream.Length));
    }
}
