using Content.Shared.Body.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Shared.Body.Systems.Part;

/// <summary>
///     Contains event handlers for networking body parts, i.e. get/handle component state.
/// </summary>
public abstract partial class SharedBodyPartSystem
{
    public void InitializeNetworking()
    {
        SubscribeLocalEvent<SharedBodyPartComponent, ComponentGetState>(OnComponentGetState);
        SubscribeLocalEvent<SharedBodyPartComponent, ComponentHandleState>(OnComponentHandleState);
    }

    private void OnComponentGetState(EntityUid uid, SharedBodyPartComponent component, ref ComponentGetState args)
    {
        var mechanismIds = new EntityUid[component.Mechanisms.Count];

        var i = 0;
        foreach (var mechanism in component.Mechanisms)
        {
            mechanismIds[i] = mechanism.Owner;
            i++;
        }

        args.State = new BodyPartComponentState(mechanismIds);
    }

    private void OnComponentHandleState(EntityUid uid, SharedBodyPartComponent component, ref ComponentHandleState args)
    {
        if (args.Current is BodyPartComponentState state)
        {
            component.Mechanisms.Clear();
            foreach (var id in state.MechanismIds)
            {
                if (!Exists(id))
                {
                    continue;
                }

                if (!TryComp(id, out MechanismComponent? mechanism))
                {
                    continue;
                }

                component.Mechanisms.Add(mechanism);
            }
        }
    }
}
