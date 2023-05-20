using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Linq;
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
using System.Collections;
using System.ComponentModel;

namespace Content.Server.Corvax.TTS;

public record ConcurrentList<T> : ICollection<T> where T : notnull
{
    private readonly ConcurrentDictionary<T, object?> _store;

    public ConcurrentList(IEnumerable<T>? items = null)
    {
        var prime = (items ?? Enumerable.Empty<T>()).Select(x => new KeyValuePair<T, object?>(x, null));
        _store = new ConcurrentDictionary<T, object?>(prime);
    }

    public IEnumerator<T> GetEnumerator()
    {
        return _store.Keys.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add(T item)
    {
        if(_store.TryAdd(item, null) == false)
            throw new ApplicationException("Unable to concurrently add item to list");
    }

    public void Clear()
    {
        _store.Clear();
    }

    public bool Contains(T item)
    {
        return _store.ContainsKey(item);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        _store.Keys.CopyTo(array, arrayIndex);
    }

    public bool Remove(T item)
    {
        return _store.Keys.Remove(item);
    }

    public int Count => _store.Count;

    public bool IsReadOnly => _store.Keys.IsReadOnly;
}

// ReSharper disable once InconsistentNaming
public sealed class TTSManager
{
    private static readonly Histogram RequestTimings = Metrics.CreateHistogram(
        "tts_req_timings",
        "Timings of TTS API requests",
        new HistogramConfiguration()
        {
            LabelNames = new[] {"type"},
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
    private readonly ConcurrentList<string> _cacheKeysSeq = new();
    private readonly ConcurrentDictionary<string, byte[]> _cacheRadio = new();
    private readonly ConcurrentList<string> _cacheRadioKeysSeq = new();

    private int _maxCachedCount = 200;

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("tts");
        _cfg.OnValueChanged(CCCVars.TTSMaxCache, val =>
        {
            _maxCachedCount = val;
            ResetCache();
        }, true);
    }

    /// <summary>
    /// Generates audio with passed text by API
    /// </summary>
    /// <param name="speaker">Identifier of speaker</param>
    /// <param name="text">SSML formatted text</param>
    /// <returns>OGG audio bytes</returns>
    /// <exception cref="Exception">Throws if url or token CCVar not set or http request failed</exception>
    public async Task<byte[]> ConvertTextToSpeech(string speaker, string text)
    {
        var url = _cfg.GetCVar(CCCVars.TTSApiUrl);
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new Exception("TTS Api url not specified");
        }

        var token = _cfg.GetCVar(CCCVars.TTSApiToken);
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new Exception("TTS Api token not specified");
        }

        WantedCount.Inc();
        var cacheKey = GenerateCacheKey(speaker, text);
        if (_cache.TryGetValue(cacheKey, out var data))
        {
            ReusedCount.Inc();
            _sawmill.Debug($"Use cached sound for '{text}' speech by '{speaker}' speaker");
            return data;
        }

        var body = new GenerateVoiceRequest
        {
            ApiToken = token,
            Text = text,
            Speaker = speaker,
        };

        var reqTime = DateTime.UtcNow;
        try
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            var response = await _httpClient.PostAsJsonAsync(url, body, cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"TTS request returned bad status code: {response.StatusCode}");
            }

            var json = await response.Content.ReadFromJsonAsync<GenerateVoiceResponse>(cancellationToken: cts.Token);
            var soundData = Convert.FromBase64String(json.Results.First().Audio);

            _cache.AddOrUpdate(cacheKey, soundData, (_, __) => soundData);
            if (!_cacheKeysSeq.Contains(cacheKey))
                _cacheKeysSeq.Add(cacheKey);
            if (_cache.Count > _maxCachedCount)
            {
                var firstKey = _cacheKeysSeq.First();
                _cache.TryRemove(firstKey, out _);
                _cacheKeysSeq.Remove(firstKey);
            }

            _sawmill.Debug($"Generated new sound for '{text}' speech by '{speaker}' speaker ({soundData.Length} bytes)");
            RequestTimings.WithLabels("Success").Observe((DateTime.UtcNow - reqTime).TotalSeconds);

            return soundData;
        }
        catch (TaskCanceledException)
        {
            RequestTimings.WithLabels("Timeout").Observe((DateTime.UtcNow - reqTime).TotalSeconds);
            _sawmill.Error($"Timeout of request generation new sound for '{text}' speech by '{speaker}' speaker");
            throw new Exception("TTS request timeout");
        }
        catch (Exception e)
        {
            RequestTimings.WithLabels("Error").Observe((DateTime.UtcNow - reqTime).TotalSeconds);
            _sawmill.Error($"Failed of request generation new sound for '{text}' speech by '{speaker}' speaker\n{e}");
            throw new Exception("TTS request failed");
        }
    }

    public async Task<byte[]> ConvertTextToSpeechRadio(string speaker, string text)
    {
        var soundData = await ConvertTextToSpeech(speaker, text);
        WantedRadioCount.Inc();
        var cacheKey = GenerateCacheKey(speaker, text);
        if (_cacheRadio.TryGetValue(cacheKey, out var cachedSoundData))
        {
            ReusedRadioCount.Inc();
            _sawmill.Debug($"Use cached radio sound for '{text}' speech by '{speaker}' speaker");
            return cachedSoundData;
        }

        var reqTime = DateTime.UtcNow;
        try
        {
            var outputFilename = Path.GetTempPath() + Guid.NewGuid() + ".ogg";
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
            if (!_cacheRadioKeysSeq.Contains(cacheKey))
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
            _sawmill.Error($"Timeout of request generation new radio sound for '{text}' speech by '{speaker}' speaker");
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
            _sawmill.Error($"Failed of request generation new radio sound for '{text}' speech by '{speaker}' speaker\n{e}");
            throw new Exception("TTS request failed");
        }
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

    private struct GenerateVoiceRequest
    {
        public GenerateVoiceRequest()
        {
        }

        [JsonPropertyName("api_token")]
        public string ApiToken { get; set; } = "";

        [JsonPropertyName("text")]
        public string Text { get; set; } = "";

        [JsonPropertyName("speaker")]
        public string Speaker { get; set; } = "";

        [JsonPropertyName("ssml")]
        // ReSharper disable once InconsistentNaming
        public bool SSML { get; private set; } = true;

        [JsonPropertyName("word_ts")]
        // ReSharper disable once InconsistentNaming
        public bool WordTS { get; private set; } = false;

        [JsonPropertyName("put_accent")]
        public bool PutAccent { get; private set; } = true;

        [JsonPropertyName("put_yo")]
        public bool PutYo { get; private set; } = false;

        [JsonPropertyName("sample_rate")]
        public int SampleRate { get; private set; } = 24000;

        [JsonPropertyName("format")]
        public string Format { get; private set; } = "ogg";
    }

    private struct GenerateVoiceResponse
    {
        [JsonPropertyName("results")]
        // ReSharper disable once CollectionNeverUpdated.Local
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public List<VoiceResult> Results { get; set; }

        [JsonPropertyName("original_sha1")]
        public string Hash { get; set; }
    }

    private struct VoiceResult
    {
        [JsonPropertyName("audio")]
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public string Audio { get; set; }
    }
}
