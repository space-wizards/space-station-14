using Content.Server.Body.Systems;
using Content.Server.DoAfter;
using Content.Shared._Impstation.Pleebnar;
using Content.Shared._Impstation.Pleebnar.Components;
using Content.Shared.Body.Components;
using Content.Shared.DoAfter;
using Content.Shared.Gibbing.Systems;
using Content.Shared.Popups;
using Robust.Shared.Physics.Components;

namespace Content.Server._Impstation.Pleebnar;
/// <summary>
/// handles the behaviour of pleebnar gibbing action
/// </summary>
public sealed class PleebnarGibSystem : SharedPleebnarGibSystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    //init function
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PleebnarGibActionComponent, PleebnarGibEvent>(PleebnarGib);
        SubscribeLocalEvent<PleebnarGibActionComponent, PleebnarGibDoAfterEvent>(PleebnarGibDoafter);
    }
    //function called when an entity is targeted by the action
    public void PleebnarGib(Entity<PleebnarGibActionComponent> ent, ref PleebnarGibEvent args)
    {
        if ((!HasComp<PleebnarGibbableComponent>(args.Target))&&(!HasComp<BodyComponent>(args.Target)))//check if it has a body and is gibbable by pleebnars, else return
        {
            return;
        }
        if (!TryComp<PhysicsComponent>(args.Target, out var targetPhysics))//try to get physics componenet, if not present return
            return;

        _popupSystem.PopupEntity(Loc.GetString("pleebnar-focus"),ent.Owner,PopupType.Small);
        var doargs = new DoAfterArgs(EntityManager, ent, targetPhysics.Mass/10, new SharedPleebnarGibSystem.PleebnarGibDoAfterEvent(), ent, args.Target)
        {
            DistanceThreshold = 15f,
            BreakOnDamage = true,
            BreakOnHandChange = false,
            BreakOnMove = true,
            BreakOnWeightlessMove = true,
            AttemptFrequency = AttemptFrequency.StartAndEnd
        };//lifted from baby kode code, basically takes longer to gib the more mass there is,
        _doAfter.TryStartDoAfter(doargs);
        args.Handled = true;
    }
    //actually handle the gibbing
    public void PleebnarGibDoafter(Entity<PleebnarGibActionComponent> ent, ref PleebnarGibDoAfterEvent args)
    {
        if (args.Target == null)//if the target somehow dissapears then return
        {
            return;
        }

        if (HasComp<PleebnarMindShieldComponent>(args.Target))//if it is protected gib the user instead
        {
            _body.GibBody(ent, true);
            return;
        }
        _body.GibBody((EntityUid)args.Target,true);
    }

}
