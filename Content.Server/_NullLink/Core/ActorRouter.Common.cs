using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Content.Server._NullLink.Helpers;
using Content.Shared.CCVar;
using Content.Shared.NullLink.CCVar;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Robust.Shared.Configuration;
using Starlight.NullLink;

namespace Content.Server._NullLink.Core;

public sealed partial class ActorRouter : IActorRouter, IDisposable
{
    public bool TryGetGrain<TGrainInterface>(
            Guid primaryKey,
            [NotNullWhen(true)] out TGrainInterface? grain,
            string? grainClassNamePrefix = null)
            where TGrainInterface : IGrainWithGuidKey
    {
        if (OrleansClientHolder.Client is { } client)
        {
            grain = client.GetGrain<TGrainInterface>(primaryKey, grainClassNamePrefix);
            return true;
        }

        grain = default;
        return false;
    }

    public bool TryGetGrain<TGrainInterface>(
            int primaryKey,
            [NotNullWhen(true)] out TGrainInterface? grain,
            string? grainClassNamePrefix = null)
            where TGrainInterface : IGrainWithIntegerKey
    {
        if (OrleansClientHolder.Client is { } client)
        {
            grain = client.GetGrain<TGrainInterface>(primaryKey, grainClassNamePrefix);
            return true;
        }

        grain = default;
        return false;
    }

    public bool TryGetGrain<TGrainInterface>(
            string primaryKey,
            [NotNullWhen(true)] out TGrainInterface? grain,
            string? grainClassNamePrefix = null)
            where TGrainInterface : IGrainWithStringKey
    {
        if (OrleansClientHolder.Client is { } client)
        {
            grain = client.GetGrain<TGrainInterface>(primaryKey, grainClassNamePrefix);
            return true;
        }

        grain = default;
        return false;
    }

    public bool TryCreateObjectReference<TGrainObserverInterface>(IGrainObserver obj, [NotNullWhen(true)] out TGrainObserverInterface? objectReference) 
        where TGrainObserverInterface : IGrainObserver
    {
        if (OrleansClientHolder.Client is { } client)
        {
            objectReference = client.CreateObjectReference<TGrainObserverInterface>(obj);
            return true;
        }

        objectReference = default;
        return false;
    }
}
