using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Actions.Components;

namespace Content.Shared._Starlight.Action;
public sealed class StarlightActionsSystem : EntitySystem
{
    public EntityUid[] HideActions(EntityUid performer, ActionsComponent? comp = null)
    {
        if (!Resolve(performer, ref comp, false))
            return [];

        var actions = comp.Actions.ToArray();
        comp.Actions.Clear();
        Dirty(performer, comp);
        return actions;
    }
    public void UnHideActions(EntityUid performer, EntityUid[] actions, ActionsComponent? comp = null)
    {
        if (!Resolve(performer, ref comp, false))
            return;

        foreach (var action in actions)
            comp.Actions.Add(action);
        Dirty(performer, comp);
    }
}
