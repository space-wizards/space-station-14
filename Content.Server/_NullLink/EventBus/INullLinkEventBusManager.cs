using System.Diagnostics.CodeAnalysis;
using Starlight.NullLink.Event;

namespace Content.Server._NullLink.EventBus;

public interface INullLinkEventBusManager
{
    void Initialize();
    void Shutdown();
    bool TryDequeue([MaybeNullWhen(false)] out BaseEvent result);
}