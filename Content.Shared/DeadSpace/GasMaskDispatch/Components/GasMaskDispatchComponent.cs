// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Actions;
using Content.Shared.Radio;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.DeadSpace.GasMaskDispatch.Components;

/// <summary>
/// Компонент противогаза, позволяющий надевшему быстро запросить подкрепление по радио.
/// Радиоканал задаётся отдельно для каждого противогаза, что позволяет переиспользовать
/// одну и ту же логику для разных фракций (СБ, ОБР, АПБТ и т.д.).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GasMaskDispatchComponent : Component
{
    /// <summary>
    /// Радиоканал, в который будет отправлено сообщение о запросе подкрепления.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<RadioChannelPrototype> Channel;

    /// <summary>
    /// Звук, воспроизводимый после отправки сообщения всем слушателям канала и отправителю.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_DeadSpace/Announcements/dispatch_please_respond.ogg");
}

/// <summary>
/// Событие открытия радиального меню запроса подкрепления, вызывается нажатием на Action.
/// </summary>
public sealed partial class OpenGasMaskDispatchMenuEvent : InstantActionEvent;

/// <summary>
/// Коды запроса подкрепления, выбираемые в радиальном меню.
/// </summary>
[Serializable, NetSerializable]
public enum GasMaskDispatchCode : byte
{
    /// <summary>Код 0 — офицер убит.</summary>
    Code0,

    /// <summary>Код 1 — офицер ранен.</summary>
    Code1,

    /// <summary>Код 2 — запрос большого подкрепления.</summary>
    Code2,

    /// <summary>Код 3 — запрос малого подкрепления.</summary>
    Code3,
}

/// <summary>
/// Сообщение от клиента о выборе кода в радиальном меню запроса подкрепления.
/// </summary>
[Serializable, NetSerializable]
public sealed class GasMaskDispatchSelectMessage(NetEntity mask, GasMaskDispatchCode code) : EntityEventArgs
{
    public readonly NetEntity Mask = mask;
    public readonly GasMaskDispatchCode Code = code;
}
