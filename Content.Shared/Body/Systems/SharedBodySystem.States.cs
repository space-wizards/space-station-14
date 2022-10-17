using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Robust.Shared.GameStates;

namespace Content.Shared.Body.Systems;

public partial class SharedBodySystem
{
    public void InitializeStateHandling()
    {
        SubscribeLocalEvent<BodyComponent, ComponentGetState>(OnBodyGetState);
        SubscribeLocalEvent<BodyComponent, ComponentHandleState>(OnBodyHandleState);

        SubscribeLocalEvent<BodyPartComponent, ComponentGetState>(OnPartGetState);
        SubscribeLocalEvent<BodyPartComponent, ComponentHandleState>(OnPartHandleState);

        SubscribeLocalEvent<OrganComponent, ComponentGetState>(OnOrganGetState);
        SubscribeLocalEvent<OrganComponent, ComponentHandleState>(OnOrganHandleState);
    }

    private void OnBodyGetState(EntityUid uid, BodyComponent body, ref ComponentGetState args)
    {
        args.State = new BodyComponentState(body.Root, body.GibSound);
    }

    private void OnBodyHandleState(EntityUid uid, BodyComponent body, ref ComponentHandleState args)
    {
        if (args.Current is not BodyComponentState state)
            return;

        body.Root = state.Root;
        body.GibSound = state.GibSound;
    }

    private void OnPartGetState(EntityUid uid, BodyPartComponent part, ref ComponentGetState args)
    {
        args.State = new BodyPartComponentState(
            part.Body,
            part.ParentSlot,
            part.Children,
            part.Organs,
            part.PartType,
            part.IsVital,
            part.Symmetry
        );
    }

    private void OnPartHandleState(EntityUid uid, BodyPartComponent part, ref ComponentHandleState args)
    {
        if (args.Current is not BodyPartComponentState state)
            return;

        part.Body = state.Body;
        part.ParentSlot = state.ParentSlot;
        part.Children = state.Children;
        part.Organs = state.Organs;
        part.PartType = state.PartType;
        part.IsVital = state.IsVital;
        part.Symmetry = state.Symmetry;
    }

    private void OnOrganGetState(EntityUid uid, OrganComponent organ, ref ComponentGetState args)
    {
        args.State = new OrganComponentState(organ.Body, organ.ParentSlot);
    }

    private void OnOrganHandleState(EntityUid uid, OrganComponent organ, ref ComponentHandleState args)
    {
        if (args.Current is not OrganComponentState state)
            return;

        organ.Body = state.Body;
        organ.ParentSlot = state.Parent;
    }
}
