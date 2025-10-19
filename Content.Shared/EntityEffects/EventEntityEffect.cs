// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

namespace Content.Shared.EntityEffects;

public abstract partial class EventEntityEffect<T> : EntityEffect where T : EntityEffect
{
    public override void Effect(EntityEffectBaseArgs args)
    {
        if (this is not T type)
            return;
        var ev = new ExecuteEntityEffectEvent<T>(type, args);
        args.EntityManager.EventBus.RaiseEvent(EventSource.Local, ref ev);
    }
}
