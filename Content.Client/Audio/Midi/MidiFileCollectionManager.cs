using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Content.Client.Audio.Midi;

/// <summary>
/// Handles storage/management of MIDI files stored inside the user data directory.
/// </summary>
[PublicAPI]
public sealed partial class MidiFileCollectionManager : IPostInjectInit
{
    /// <summary>
    /// Directory path to use inside UserData for storing MIDIs.
    /// </summary>
    private static readonly ResPath UserMidiDirectory = new("/UserMidis/");

    private const string SawmillCategory = "midifilecollection";

    [Dependency] private IResourceManager _resManager = default!;
    [Dependency] private ILogManager _logManager = default!;

    private ISawmill _sawmill = default!;

    private readonly List<ResPath> _filePaths = [];

    /// <summary>
    /// Raised after a MIDI file has been added to the library. Contains added file path as argument.
    /// </summary>
    public event Action<ResPath>? MidiFileAdded;

    /// <summary>
    /// Raised after a MIDI file has been removed from the library. Contains removed file path as argument.
    /// </summary>
    public event Action<ResPath>? MidiFileRemoved;

    /// <summary>
    /// Raised after more than one or two library changes occured at once. (i.e. initial load or when all items deleted)
    /// </summary>
    public event Action? MidiFilesReset;

    ///<inheritdoc cref="IPostInjectInit"/>
    public void PostInject()
    {
        EnsureMidiDirectoryExists();
        ReloadLibrary();
        _sawmill = _logManager.GetSawmill(SawmillCategory);
    }

    /// <summary>
    /// Returns the binary content of the given MIDI file.
    /// </summary>
    /// <param name="filePath">MIDI file path to get.</param>
    /// <returns>MIDI binary as a byte array or an empty byte array if the file doesn't exist.</returns>
    public byte[] GetMidiData(ResPath filePath)
    {
        try
        {
            var fullPath = UserMidiDirectory / filePath;
            return _resManager.UserData.ReadAllBytes(fullPath);
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to read MIDI data from '{filePath}': {e.Message}");
            return [];
        }
    }

    /// <summary>
    /// Returns an enumeration of all available MIDI files.
    /// </summary>
    /// <returns>An enumeration of MIDI file paths.</returns>
    public IEnumerable<ResPath> GetMidiFiles()
    {
        return _filePaths;
    }

    /// <summary>
    /// Stores the given byte stream with the given file path inside the <see cref="UserMidiDirectory"/> directory.
    /// </summary>
    /// <param name="filePath">File path to write.</param>
    /// <param name="data">Binary data to write.</param>
    /// <remarks>Raises <see cref="MidiFileAdded"/> on success.</remarks>
    public async Task<bool> AddMidiFile(ResPath filePath, Stream data)
    {
        try
        {
            await using var file = _resManager.UserData.OpenWrite(UserMidiDirectory / filePath);
            await data.CopyToAsync(file);
            _filePaths.Add(filePath);
            MidiFileAdded?.Invoke(filePath);
            return true;
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to store MIDI file '{filePath}' in library: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Stores the given byte array with the given file path inside the <see cref="UserMidiDirectory"/> directory.
    /// </summary>
    /// <param name="filePath">File path to write.</param>
    /// <param name="data">Binary data to write.</param>
    /// <remarks>Raises <see cref="MidiFileAdded"/> on success.</remarks>
    public async Task<bool> AddMidiFile(ResPath filePath, byte[] data)
    {
        return await AddMidiFile(filePath, new MemoryStream(data));
    }

    /// <summary>
    /// Renames a MIDI file inside the library.
    /// </summary>
    /// <param name="oldPath">Current file path</param>
    /// <param name="newPath">New file path</param>
    /// <remarks>Raises <see cref="MidiFileRemoved"/> and <see cref="MidiFileAdded"/> on success.</remarks>
    public bool RenameMidiFile(ResPath oldPath, ResPath newPath)
    {
        try
        {
            var fullOldPath = UserMidiDirectory / oldPath;
            var fullNewPath = UserMidiDirectory / newPath;
            fullOldPath = fullOldPath.Clean();
            fullNewPath = fullNewPath.Clean();
            _resManager.UserData.Rename(fullOldPath, fullNewPath);
            _filePaths.Remove(oldPath);
            MidiFileRemoved?.Invoke(oldPath);
            _filePaths.Add(newPath);
            MidiFileAdded?.Invoke(newPath);
            return true;
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to rename MIDI file '{oldPath}' with '{newPath}': {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Permanently removes the given MIDI file if it exists inside <see cref="UserMidiDirectory"/>.
    /// </summary>
    /// <param name="filePath">File path to remove.</param>
    /// <remarks>Raises <see cref="MidiFileRemoved"/></remarks>
    public void RemoveMidiFile(ResPath filePath)
    {
        DeleteMidiFile(filePath);
        _filePaths.Remove(filePath);
        MidiFileRemoved?.Invoke(filePath);
    }

    /// <summary>
    /// Removes all registered MIDI files, permanently.
    /// </summary>
    /// <remarks>Raises <see cref="MidiFilesReset"/></remarks>
    public void RemoveAllMidiFiles()
    {
        foreach (var path in _filePaths)
        {
            DeleteMidiFile(path);
        }

        _filePaths.Clear();
        MidiFilesReset?.Invoke();
    }

    /// <summary>
    /// Clears and reloads the entire MIDI library.
    /// </summary>
    public void ReloadLibrary()
    {
        _filePaths.Clear();
        if (!_resManager.UserData.IsDir(UserMidiDirectory))
            return;

        foreach (var path in _resManager.UserData.DirectoryEntries(UserMidiDirectory))
        {
            var filePath = new ResPath(UserMidiDirectory + path);
            if (!filePath.Extension.Equals("midi") && !filePath.Extension.Equals("mid"))
                continue;

            _filePaths.Add(new ResPath(path));
        }

        MidiFilesReset?.Invoke();
    }

    private void DeleteMidiFile(ResPath filePath)
    {
        var path = (UserMidiDirectory / filePath).Clean();
        _resManager.UserData.Delete(path);
    }

    private void EnsureMidiDirectoryExists()
    {
        if (!_resManager.UserData.Exists(UserMidiDirectory))
            _resManager.UserData.CreateDir(UserMidiDirectory);
    }
}
