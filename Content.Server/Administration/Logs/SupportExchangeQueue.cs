using Content.Server.Database;

namespace Content.Server.Administration.Logs;

/// <summary>
///⣿⣿⣿⣿⣿⣿⡿⠛⣛⣛⣛⣛⣛⣛⣛⣛⣛⣛⡛⠛⠿⠿⢿⣿⣿⣿⣿⣿⣿<br/>
///⣿⣿⣿⣿⡿⢃⣴⣿⠿⣻⢽⣲⠿⠭⠭⣽⣿⣓⣛⣛⣓⣲⣶⣢⣍⠻⢿⣿⣿<br/>
///⣿⣿⣿⡿⢁⣾⣿⣵⡫⣪⣷⠿⠿⢿⣷⣹⣿⣿⣿⢲⣾⣿⣾⡽⣿⣷⠈⣿⣿<br/>
///⣿⣿⠟⠁⣚⣿⣿⠟⡟⠡⠀⠀⠀⠶⣌⠻⣿⣿⠿⠛⠉⠉⠉⢻⣿⣿⠧⡙⢿<br/>
///⡿⢡⢲⠟⣡⡴⢤⣉⣛⠛⣋⣥⣿⣷⣦⣾⣿⣿⡆⢰⣾⣿⠿⠟⣛⡛⢪⣎⠈<br/>
///⣧⢸⣸⠐⣛⡁⢦⣍⡛⠿⢿⣛⣿⡍⢩⠽⠿⣿⣿⡦⠉⠻⣷⣶⠇⢻⣟⠟⢀<br/>
///⣿⣆⠣⢕⣿⣷⡈⠙⠓⠰⣶⣤⣍⠑⠘⠾⠿⠿⣉⣡⡾⠿⠗⡉⡀⠘⣶⢃⣾<br/>
///⣿⣿⣷⡈⢿⣿⣿⣌⠳⢠⣄⣈⠉⠘⠿⠿⠆⠶⠶⠀⠶⠶⠸⠃⠁⠀⣿⢸⣿<br/>
///⣿⣿⣿⣷⡌⢻⣿⣿⣧⣌⠻⢿⢃⣷⣶⣤⢀⣀⣀⢀⣀⠀⡀⠀⠀⢸⣿⢸⣿<br/>
///⣿⣿⣿⣿⣿⣦⡙⠪⣟⠭⣳⢦⣬⣉⣛⠛⠘⠿⠇⠸⠋⠘⣁⣁⣴⣿⣿⢸⣿<br/>
///⣿⣿⣿⣿⣿⣿⣿⣷⣦⣉⠒⠭⣖⣩⡟⠛⣻⣿⣿⣿⣿⣿⣟⣫⣾⢏⣿⠘⣿<br/>
///⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣶⣤⣍⡛⠿⠿⣶⣶⣿⣿⣿⣿⣿⣾⣿⠟⣰⣿<br/>
///⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣷⣶⣶⣤⣭⣍⣉⣛⣋⣭⣥⣾⣿⣿<br/><br/>
/// Queues up messages to be stored for an exchange and indicates if the exchange itself has been stored yet.
/// </summary>
/// <remarks>The messages should only be stored after <see cref="Stored"/> has been set to true</remarks>
public sealed class SupportExchangeQueue
{
    public bool Stored { get; set; }

    public int SupportExchangeId { get; set; }

    public Queue<SupportMessage> MessageQueue { get; } = new();
}
