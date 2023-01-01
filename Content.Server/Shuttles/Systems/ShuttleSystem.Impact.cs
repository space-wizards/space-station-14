using Content.Server.Shuttles.Components;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;

namespace Content.Server.Shuttles.Systems;

public sealed partial class ShuttleSystem
{
    /// <summary>
    /// Minimum velocity difference between 2 bodies for a shuttle "impact" to occur.
    /// </summary>
    private const int MinimumImpactVelocity = 10;

    private readonly SoundCollectionSpecifier _shuttleImpactSound = new("ShuttleImpactSound");

    private void InitializeImpact()
    {
        SubscribeLocalEvent<ShuttleComponent, StartCollideEvent>(OnShuttleCollide);
    }

    private void OnShuttleCollide(EntityUid uid, ShuttleComponent component, ref StartCollideEvent args)
    {
        var ourBody = args.OurFixture.Body;
        var otherBody = args.OtherFixture.Body;

        if (!HasComp<ShuttleComponent>(otherBody.Owner))
            return;

        // TODO: Would also be nice to have a continuous sound for scraping.
        var ourXform = Transform(ourBody.Owner);

        if (ourXform.MapUid == null)
            return;

        var otherXform = Transform(otherBody.Owner);

        var ourPoint = ourXform.InvWorldMatrix.Transform(args.WorldPoint);
        var otherPoint = otherXform.InvWorldMatrix.Transform(args.WorldPoint);

        var ourVelocity = _physics.GetLinearVelocity(ourBody.Owner, ourPoint, ourBody, ourXform);
        var otherVelocity = _physics.GetLinearVelocity(otherBody.Owner, otherPoint, otherBody, otherXform);
        var jungleDiff = (ourVelocity - otherVelocity).Length;

        if (jungleDiff < MinimumImpactVelocity)
        {
            return;
        }

        var coordinates = new EntityCoordinates(ourXform.MapUid.Value, args.WorldPoint);
        var volume = MathF.Min(10f, 1f * MathF.Pow(jungleDiff, 0.5f) - 5f);
        var audioParams = AudioParams.Default.WithVariation(0.05f).WithVolume(volume);

        _audio.Play(_shuttleImpactSound, Filter.Pvs(coordinates, rangeMultiplier: 4f, entityMan: EntityManager), coordinates, true, audioParams);
    }
}
