using System.Threading.Tasks;
using Orleans;
using Starlight.NullLink;

namespace Content.Server._NullLink.Core;
public interface IActorRouter
{
    Task Connection { get; }
    bool Enabled { get; }

    void Initialize();
    ValueTask Shutdown();
    bool TryCreateObjectReference<TGrainObserverInterface>(IGrainObserver obj, out TGrainObserverInterface? objectReference) where TGrainObserverInterface : IGrainObserver;
    bool TryGetGrain<TGrainInterface>(Guid primaryKey, out TGrainInterface? grain, string? grainClassNamePrefix = null) where TGrainInterface : IGrainWithGuidKey;
    bool TryGetGrain<TGrainInterface>(int primaryKey, out TGrainInterface? grain, string? grainClassNamePrefix = null) where TGrainInterface : IGrainWithIntegerKey;
    bool TryGetGrain<TGrainInterface>(string primaryKey, out TGrainInterface? grain, string? grainClassNamePrefix = null) where TGrainInterface : IGrainWithStringKey;
    bool TryGetServerGrain(out IServerGrain? serverGrain);
}