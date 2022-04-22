using Content.Server.Wires;
using Content.Shared.Doors.Components;
using Content.Shared.Wires;

namespace Content.Server.Doors;

// really really hacky way of doing this,
// but unless multi-count wires are implemented
// (so, skill issue)
// this is the 'best i can do'
//
// if i don't do multi count wires by then:
//
// The general idea I had was that wires could have a 'count'
// variable in prototype at first: however, this wouldn't
// allow for each action to have its own set variables (see:
// timeout)
//
// So, what *could* occur is that when a wire is created,
// it's given a special identifier based on what wire number it is.
// This could be hardcoded (another enum) or dynamic (has to be
// a string unless you want to risk hash collision by just doing
// hash + 1, but that might just be rustbrain talking).
// The objects will process these identifiers when
// performing their logic, and then act accordingly.
//
// This could be done by replacing the 'Identifier' attribute
// with a function that assigns wire identifiers based on the
// current wire count of that given wire's type, which is
// recorded in WiresSystem during wire layout initialization.
//
// This does however, come with the consequence that all wires
// actions must track multi count wiring through every single
// state change. This could also be tracked above in WiresComponent,
// and within the WiresSystem so that it could easily be
// exposed as an API:
//
// i.e.,
// WiresComponent.CutWireCount
// - identifier : wires cut (+/- when wires are cut/mended)
//
// An alternative to this would be making Identifier objects
// nullable upon querying for a wire's logical identifier,
// which would then cause it to be removed from the
// wire list.
//
// DoorPowerWireAction, in essence, could just be replaced
// with two instances of PowerWireAction with differing
// timeout settings.
public abstract class BaseDoorPowerWireAction : BaseWireAction
{
    private string _text = "POWR";
    public virtual int _timeout { get; } = 30;

    public override object StatusKey { get; } = DoorVisuals.Powered;

    public override StatusLightData? GetStatusLightData(Wire wire)
    {
        var lightState = StatusLightState.Off;

        return new StatusLightData(
            Color.Gold,
            lightState,
            _text);

    }

    public override bool Cut(EntityUid user, Wire wire)
    {

        return true;
    }

    public override bool Mend(EntityUid user, Wire wire)
    {

        return true;
    }

    public override bool Pulse(EntityUid user, Wire wire)
    {

        return true;
    }

    public override void Update(Wire wire)
    {

    }
}

[DataDefinition]
public sealed class MainDoorPowerWireAction : BaseDoorPowerWireAction
{
    public override object Identifier { get; } = DoorPowerWireActionKeys.Main;
}

[DataDefinition]
public sealed class BackupDoorPowerWireAction : BaseDoorPowerWireAction
{
    public override int _timeout { get; } = 15;

    public override object Identifier { get; } = DoorPowerWireActionKeys.Backup;

    public override StatusLightData? GetStatusLightData(Wire wire)
    {
        return null;
    }
}

public enum DoorPowerWireActionKeys
{
    Main,
    Backup
}
