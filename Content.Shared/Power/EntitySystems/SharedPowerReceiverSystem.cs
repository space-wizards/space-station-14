using Content.Shared.Examine;
using Content.Shared.Power.Components;

namespace Content.Shared.Power.EntitySystems;

public abstract class SharedPowerReceiverSystem : EntitySystem
{
    ///<summary>
    ///Adds some markup to the examine text of whatever object is using this component to tell you if it's powered or not, even if it doesn't have an icon state to do this for you.
    ///</summary>
    protected void OnExamined(EntityUid uid, SharedApcPowerReceiverComponent component, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("power-receiver-component-on-examine-main",
                                        ("stateText", Loc.GetString(component.Powered
                                            ? "power-receiver-component-on-examine-powered"
                                            : "power-receiver-component-on-examine-unpowered"))));
    }
}
