﻿using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.Containers;

namespace Content.Shared.Silicons.Borgs;

public partial class SharedBorgSystem
{
    public virtual void InitializeMMI()
    {
        SubscribeLocalEvent<MMIComponent, ContainerIsInsertingAttemptEvent>(OnMMIAttemptInsert);
    }

    private void OnMMIAttemptInsert(Entity<MMIComponent> entity, ref ContainerIsInsertingAttemptEvent args)
    {
        var ev = new AttemptMakeBrainIntoSiliconEvent(args.EntityUid, entity);
        RaiseLocalEvent(args.EntityUid, ref ev);
        if (ev.Cancelled)
            args.Cancel();
    }
}
