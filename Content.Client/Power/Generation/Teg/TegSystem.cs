using Content.Client.Examine;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

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
    [ValidatePrototypeId<EntityPrototype>]
    private const string ArrowPrototype = "TegCirculatorArrow";

    public override void Initialize()
    {
        SubscribeLocalEvent<TegCirculatorComponent, ClientExaminedEvent>(CirculatorExamined);
    }

    private void CirculatorExamined(EntityUid uid, TegCirculatorComponent component, ClientExaminedEvent args)
    {
        Spawn(ArrowPrototype, new EntityCoordinates(uid, 0, 0));
    }
}
