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
            out TGrainInterface? grain,
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
            out TGrainInterface? grain,
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
            out TGrainInterface? grain,
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

    public bool TryCreateObjectReference<TGrainObserverInterface>(IGrainObserver obj, out TGrainObserverInterface? objectReference) 
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
