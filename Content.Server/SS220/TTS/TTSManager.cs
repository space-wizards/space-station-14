using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared.Corvax.CCCVars;
using FFMpegCore;
using FFMpegCore.Arguments;
using FFMpegCore.Pipes;
using Prometheus;
using Robust.Shared.Configuration;
using System.ComponentModel;
using System.Collections.Specialized;
using System.Web;

namespace Content.Server.SS220.TTS;

// ReSharper disable once InconsistentNaming
public sealed class TTSManager
{
    private static readonly Histogram RequestTimings = Metrics.CreateHistogram(
        "tts_req_timings",
        "Timings of TTS API requests",
        new HistogramConfiguration()
        {
            LabelNames = new[] { "type" },
            Buckets = Histogram.ExponentialBuckets(.1, 1.5, 10),
        });

    private static readonly Counter WantedCount = Metrics.CreateCounter(
        "tts_wanted_count",
        "Amount of wanted TTS audio.");

    private static readonly Counter ReusedCount = Metrics.CreateCounter(
        "tts_reused_count",
        "Amount of reused TTS audio from cache.");

    private static readonly Counter WantedRadioCount = Metrics.CreateCounter(
        "tts_wanted_radio_count",
        "Amount of wanted TTS audio.");

    private static readonly Counter ReusedRadioCount = Metrics.CreateCounter(
        "tts_reused_radio_count",
        "Amount of reused TTS audio from cache.");

    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private readonly HttpClient _httpClient = new();

    private ISawmill _sawmill = default!;
    private readonly ConcurrentDictionary<string, byte[]> _cache = new();
    private readonly HashSet<string> _cacheKeysSeq = new();
    private readonly ConcurrentDictionary<string, byte[]> _cacheRadio = new();
    private readonly HashSet<string> _cacheRadioKeysSeq = new();

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new();
    private float _timeout = 1;

    private int _maxCachedCount = 200;
    private string _apiUrl = string.Empty;
    private string _apiToken = string.Empty;

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("tts");
        _cfg.OnValueChanged(CCCVars.TTSMaxCache, val =>
        {
            _maxCachedCount = val;
            ResetCache();
        }, true);
        _cfg.OnValueChanged(CCCVars.TTSRequestTimeout, val => _timeout = val, true);
        _cfg.OnValueChanged(CCCVars.TTSApiUrl, v => _apiUrl = v, true);
        _cfg.OnValueChanged(CCCVars.TTSApiToken, v =>
        {
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", v);
            _apiToken = v;
        },
        true);
    }

    /// <summary>
    /// Generates audio with passed text by API
    /// </summary>
    /// <param name="speaker">Identifier of speaker</param>
    /// <param name="text">SSML formatted text</param>
    /// <returns>WAV audio bytes or null if failed</returns>
    public async Task<byte[]?> ConvertTextToSpeech(string speaker, string text)
    {
        WantedCount.Inc();
        var cacheKey = GenerateCacheKey(speaker, text);

        return await ExecuteWithNamedLockAsync(cacheKey, async () =>
        {
            if (_cache.TryGetValue(cacheKey, out var data))
            {
                ReusedCount.Inc();
                _sawmill.Debug($"Use cached sound for '{text}' speech by '{speaker}' speaker");
                return data;
            }

            _sawmill.Debug($"Generate new audio for '{text}' speech by '{speaker}' speaker");
            var reqTime = DateTime.UtcNow;
            try
            {
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_timeout));

                var requestUrl = $"{_apiUrl}" + ToQueryString(new NameValueCollection() {
                    { "speaker", speaker },
                    { "text", text },
                    { "ext", "wav" }});

                var response = await _httpClient.GetAsync(requestUrl, cts.Token);
                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        _sawmill.Warning("TTS request was rate limited");
                        return null;
                    }

                    _sawmill.Error($"TTS request returned bad status code: {response.StatusCode}");
                    return null;
                }

                var soundData = await response.Content.ReadAsByteArrayAsync();

                _cache.AddOrUpdate(cacheKey, soundData, (_, __) => soundData);
                _cacheKeysSeq.Add(cacheKey);
                if (_cache.Count > _maxCachedCount)
                {
                    var firstKey = _cacheKeysSeq.First();
                    _cache.TryRemove(firstKey, out _);
                    _cacheKeysSeq.Remove(firstKey);
                }

                _sawmill.Debug(
                    $"Generated new sound for '{text}' speech by '{speaker}' speaker ({soundData.Length} bytes)");
                RequestTimings.WithLabels("Success").Observe((DateTime.UtcNow - reqTime).TotalSeconds);

                return soundData;
            }
            catch (TaskCanceledException)
            {
                RequestTimings.WithLabels("Timeout").Observe((DateTime.UtcNow - reqTime).TotalSeconds);
                _sawmill.Error($"Timeout of request generation new audio for '{text}' speech by '{speaker}' speaker");
                return null;
            }
            catch (Exception e)
            {
                RequestTimings.WithLabels("Error").Observe((DateTime.UtcNow - reqTime).TotalSeconds);
                _sawmill.Error(
                    $"Failed of request generation new sound for '{text}' speech by '{speaker}' speaker\n{e}");
                return null;
            }
        });
    }

    private static string ToQueryString(NameValueCollection nvc)
    {
        var array = (
            from key in nvc.AllKeys
            from value in nvc.GetValues(key) ?? Array.Empty<string>()
            select $"{key}={HttpUtility.UrlEncode(value)}"
            ).ToArray();

        return "?" + string.Join("&", array);
    }

    public async Task<byte[]?> ConvertTextToSpeechRadio(string speaker, string text)
    {
        WantedRadioCount.Inc();

        var cacheKey = GenerateCacheKey($"radio-{speaker}", text);
        return await ExecuteWithNamedLockAsync(cacheKey, async () =>
        {
            if (_cacheRadio.TryGetValue(cacheKey, out var cachedSoundData))
            {
                ReusedRadioCount.Inc();
                _sawmill.Debug($"Use cached radio sound for '{text}' speech by '{speaker}' speaker");
                return cachedSoundData;
            }

            var soundData = await ConvertTextToSpeech(speaker, text);
            if (soundData == null)
                return null;

            var reqTime = DateTime.UtcNow;
            try
            {
                var outputFilename = Path.GetTempPath() + Guid.NewGuid() + ".wav";
                await FFMpegArguments
                    .FromPipeInput(new StreamPipeSource(new MemoryStream(soundData)))
                    .OutputToFile(outputFilename, true, options =>
                        options.WithAudioFilters(filterOptions =>
                            {
                                filterOptions
                                    .HighPass(frequency: 1000D)
                                    .LowPass();
                                filterOptions.Arguments.Add(
                                    new CrusherFilterArgument(levelIn: 1, levelOut: 1, bits: 50, mix: 0, mode: "log")
                                );
                            }
                        )
                    ).ProcessAsynchronously();
                soundData = await File.ReadAllBytesAsync(outputFilename);
                try
                {
                    File.Delete(outputFilename);
                }
                catch (Exception _)
                {
                    // ignored
                }

                _cacheRadio.AddOrUpdate(cacheKey, soundData, (_, __) => soundData);
                _cacheRadioKeysSeq.Add(cacheKey);
                if (_cacheRadio.Count > _maxCachedCount)
                {
                    var firstKey = _cacheRadioKeysSeq.First();
                    _cacheRadio.TryRemove(firstKey, out _);
                    _cacheRadioKeysSeq.Remove(firstKey);
                }

                _sawmill.Debug(
                    $"Generated new radio sound for '{text}' speech by '{speaker}' speaker ({soundData.Length} bytes)");
                RequestTimings.WithLabels("Success").Observe((DateTime.UtcNow - reqTime).TotalSeconds);

                return soundData;
            }
            catch (TaskCanceledException)
            {
                RequestTimings.WithLabels("Timeout").Observe((DateTime.UtcNow - reqTime).TotalSeconds);
                _sawmill.Error(
                    $"Timeout of request generation new radio sound for '{text}' speech by '{speaker}' speaker");
                throw new Exception("TTS request timeout");
            }
            catch (Win32Exception e)
            {
                _sawmill.Error($"FFMpeg is not installed");
                throw new Exception("ffmpeg is not installed!");
            }
            catch (Exception e)
            {
                RequestTimings.WithLabels("Error").Observe((DateTime.UtcNow - reqTime).TotalSeconds);
                _sawmill.Error(
                    $"Failed of request generation new radio sound for '{text}' speech by '{speaker}' speaker\n{e}");
                throw new Exception("TTS request failed");
            }
        });
    }

    public void ResetCache()
    {
        _cache.Clear();
        _cacheKeysSeq.Clear();
        _cacheRadio.Clear();
        _cacheRadioKeysSeq.Clear();
    }

    private string GenerateCacheKey(string speaker, string text)
    {
        var key = $"{speaker}/{text}";
        byte[] keyData = Encoding.UTF8.GetBytes(key);
        var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = sha256.ComputeHash(keyData);
        return Convert.ToHexString(bytes);
    }

    private async Task<TResult> ExecuteWithNamedLockAsync<TResult>(string key, Func<Task<TResult>> function)
    {
        var semaphore = Locks.GetOrAdd(key, new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync();
        try
        {
            return await function();
        }
        finally
        {
            semaphore.Release();
            Locks.TryRemove(key, out _);
        }
    }

    private sealed class CrusherFilterArgument : IAudioFilterArgument
    {
        private readonly Dictionary<string, string> _arguments = new Dictionary<string, string>();

        public CrusherFilterArgument(
            double levelIn = 1f,
            double levelOut = 1f,
            int bits = 1,
            double mix = 1.0,
            string mode = "log")
        {
            _arguments.Add("level_in", levelIn.ToString("0", CultureInfo.InvariantCulture));
            _arguments.Add("level_out", levelOut.ToString("0", CultureInfo.InvariantCulture));
            _arguments.Add("bits", bits.ToString("0", CultureInfo.InvariantCulture));
            _arguments.Add("mix", mix.ToString("0", CultureInfo.InvariantCulture));
            _arguments.Add("mode", mode);
        }

        public string Key => "acrusher";

        public string Value => string.Join(":", _arguments.Select<KeyValuePair<string, string>, string>(pair => pair.Key + "=" + pair.Value));
    }
}
