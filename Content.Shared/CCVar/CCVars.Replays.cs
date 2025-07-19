using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    ///     Whether or not to record admin chat. If replays are being publicly distributes, this should probably be
    ///     false.
    /// </summary>
    public static readonly CVarDef<bool> ReplayRecordAdminChat =
        CVarDef.Create("replay.record_admin_chat", false, CVar.ARCHIVE);

    /// <summary>
    ///     Automatically record full rounds as replays.
    /// </summary>
    public static readonly CVarDef<bool> ReplayAutoRecord =
        CVarDef.Create("replay.auto_record", false, CVar.SERVERONLY);

    /// <summary>
    ///     The file name to record automatic replays to. The path is relative to <see cref="CVars.ReplayDirectory"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     If the path includes slashes, directories will be automatically created if necessary.
    /// </para>
    /// <para>
    ///     A number of substitutions can be used to automatically fill in the file name: <c>{year}</c>, <c>{month}</c>, <c>{day}</c>, <c>{hour}</c>, <c>{minute}</c>, <c>{round}</c>.
    /// </para>
    /// </remarks>
    public static readonly CVarDef<string> ReplayAutoRecordName =
        CVarDef.Create("replay.auto_record_name",
            "{year}_{month}_{day}-{hour}_{minute}-round_{round}.zip",
            CVar.SERVERONLY);

    /// <summary>
    ///     Path that, if provided, automatic replays are initially recorded in.
    ///     When the recording is done, the file is moved into its final destination.
    ///     Unless this path is rooted, it will be relative to <see cref="CVars.ReplayDirectory"/>.
    /// </summary>
    public static readonly CVarDef<string> ReplayAutoRecordTempDir =
        CVarDef.Create("replay.auto_record_temp_dir", "", CVar.SERVERONLY);
}
