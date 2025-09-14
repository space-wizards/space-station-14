using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Starlight.Antags.Abductor;
using Content.Shared.Whitelist;
using Robust.Shared.Animations;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.Computers.RemoteEye;
[RegisterComponent, NetworkedComponent, Access(typeof(SharedRemoteEyeSystem))]
public sealed partial class RemoteEyeConsoleComponent : Component
{
    [DataField(readOnly: true)]
    public EntProtoId RemoteEntityProto;

    [DataField]
    public EntityUid? RemoteEntity; 

    [DataField(readOnly: true)]
    public EntityWhitelist? Whitelist;

    [DataField(readOnly: true)]
    public EntProtoId<ActionComponent>[] Actions = [];

    [DataField]
    public Color Color { get; set; } = Color.White;
}
