using System.Threading.Tasks;

namespace Content.Server.Starlight.TextToSpeech;
public interface ITTSManager
{
    Task<byte[]?> ConvertTextToSpeechAnnounce(int voice, string text);
    Task<byte[]?> ConvertTextToSpeechRadio(int voice, string text);
    Task<byte[]?> ConvertTextToSpeechStandard(int voice, string text);
    void Initialize();
}