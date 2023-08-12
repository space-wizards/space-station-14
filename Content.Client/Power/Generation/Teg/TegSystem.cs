using Content.Client.Examine;
using Robust.Shared.Map;

namespace Content.Client.Power.Generation.Teg;

/// <summary>
/// Handles client-side logic for the thermo-electric generator (TEG).
/// </summary>
/// <remarks>
/// <para>
/// TEG circulators show which direction the in- and outlet port is by popping up two floating arrows when examined.
/// </para>
/// </remarks>
/// <seealso cref="TegCirculatorComponent"/>
public sealed class TegSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<TegCirculatorComponent, ClientExaminedEvent>(CirculatorExamined);
    }

    private void CirculatorExamined(EntityUid uid, TegCirculatorComponent component, ClientExaminedEvent args)
    {
        Spawn("TegCirculatorArrow", new EntityCoordinates(uid, 0, 0));
    }
}
