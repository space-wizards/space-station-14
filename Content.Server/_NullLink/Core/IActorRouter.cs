using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Orleans;
using Starlight.NullLink;

namespace Content.Server._NullLink.Core;
public interface IActorRouter
{
    Task Connection { get; }
    bool Enabled { get; }

    event Action OnConnected;

    void Initialize();
    ValueTask Shutdown();
    bool TryCreateObjectReference<TGrainObserverInterface>(IGrainObserver obj, [NotNullWhen(true)] out TGrainObserverInterface? objectReference) where TGrainObserverInterface : IGrainObserver;
    bool TryGetGrain<TGrainInterface>(Guid primaryKey, [NotNullWhen(true)] out TGrainInterface? grain, string? grainClassNamePrefix = null) where TGrainInterface : IGrainWithGuidKey;
    bool TryGetGrain<TGrainInterface>(int primaryKey, [NotNullWhen(true)] out TGrainInterface? grain, string? grainClassNamePrefix = null) where TGrainInterface : IGrainWithIntegerKey;
    bool TryGetGrain<TGrainInterface>(string primaryKey, [NotNullWhen(true)] out TGrainInterface? grain, string? grainClassNamePrefix = null) where TGrainInterface : IGrainWithStringKey;
    bool TryGetServerGrain([NotNullWhen(true)] out IServerGrain? serverGrain);
}