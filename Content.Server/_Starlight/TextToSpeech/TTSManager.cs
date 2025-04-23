using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared.Starlight;
using Content.Shared.Starlight.CCVar;
using Prometheus;
using Robust.Shared.Configuration;
using System.IO;
using System;
using NAudio.Wave;
using NAudio.Dsp;
using NAudio.Wave.SampleProviders;
using NAudio.Mixer;
using OggVorbisEncoder;
using System.Runtime.CompilerServices;

namespace Content.Server.Starlight.TextToSpeech;

public sealed class TTSManager : ITTSManager
{
    [Robust.Shared.IoC.Dependency] private readonly IConfigurationManager _cfg = default!;

    private static readonly Histogram RequestTime = Metrics.CreateHistogram(
        "tts_time",
        "Time of TTS API requests",
        new HistogramConfiguration()
        {
            LabelNames = ["type"],
            Buckets = Histogram.ExponentialBuckets(.1, 1.5, 10),
        });
    private static readonly Counter RequestedCount = Metrics.CreateCounter(
       "tts_count",
       "Number of all requested TTS audio.");

    private static readonly Counter RequestedStandardCount = Metrics.CreateCounter(
        "tts_standard_count",
        "Number of requested TTS standard audio.");

    private static readonly Counter RequestedRadioCount = Metrics.CreateCounter(
        "tts_radio_count",
        "Number of requested TTS radio audio.");

    private static readonly Counter RequestedAnnounceCount = Metrics.CreateCounter(
        "tts_announce_count",
        "Number of requested TTS announce audio.");

    private readonly HttpClient _httpClient = new();
    private ISawmill _sawmill = default!;
    private string _apiUrl = string.Empty;
    private string _apiToken = string.Empty;
    private int _timeout = 7;

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("tts");
        _cfg.OnValueChanged(StarlightCCVars.TTSApiUrl, x => _apiUrl = x, true);
        _cfg.OnValueChanged(StarlightCCVars.TTSApiToken, x => _apiToken = x, true);
        _cfg.OnValueChanged(StarlightCCVars.TTSApiTimeout, x => _timeout = x, true);
    }

    public async Task<byte[]?> ConvertTextToSpeechStandard(int voice, string text)
    {
        RequestedStandardCount.Inc();
        return await ConvertTextToSpeech(voice, text);
    }
    public async Task<byte[]?> ConvertTextToSpeechRadio(int voice, string text)
    {
        RequestedRadioCount.Inc();
        return await ConvertTextToSpeech(voice, text, true);
    }
    public async Task<byte[]?> ConvertTextToSpeechAnnounce(int voice, string text)
    {
        RequestedAnnounceCount.Inc();
        return await ConvertTextToSpeech(voice, text, true);
    }
    private async Task<byte[]?> ConvertTextToSpeech(int voiceId, string text, bool isRadio = false)
    {
        RequestedCount.Inc();
        _sawmill.Verbose($"Generate new audio for '{text}' speech by voice ID '{voiceId}'");
        var stopwatch = Stopwatch.StartNew();

        var jsonBody = JsonSerializer.Serialize(new TTSRequest
        {
            VoiceId = voiceId,
            Text = text,
            PitchShift = 1.0,
            SpeedMultiplier = 1.0,
            Effect = isRadio ? Effect.Radio : 0,
        });
        var content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json");
        content.Headers.Add("X-Api-Key", _apiToken);
        var requestUrl = string.Format(_apiUrl, _apiToken);

        try
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_timeout));
            var response = await _httpClient.PostAsync(_apiUrl, content, cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                _sawmill.Error($"TTS request returned bad status code: {response.StatusCode}");
                return null;
            }

            var audio = await response.Content.ReadAsByteArrayAsync(cts.Token);

            _sawmill.Debug($"Generated new audio for '{text}' speech by voice ID '{voiceId}' ({audio.Length} bytes)");
            RequestTime.WithLabels("Success").Observe(stopwatch.Elapsed.TotalSeconds);

            return audio;
        }
        catch (TaskCanceledException)
        {
            RequestTime.WithLabels("Timeout").Observe(stopwatch.Elapsed.TotalSeconds);
            _sawmill.Error($"Timeout of request generation new audio for '{text}' speech by voice ID '{voiceId}'");
            return null;
        }
        catch (Exception e)
        {
            RequestTime.WithLabels("Error").Observe(stopwatch.Elapsed.TotalSeconds);
            _sawmill.Error($"Failed of request generation new sound for '{text}' speech by voice ID '{voiceId}' with error: {e}");
            return null;
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    private record TTSRequest
    {
        [JsonPropertyName("voiceId")]
        public int VoiceId { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; } = null!;

        [JsonPropertyName("pitchShift")]
        public double PitchShift { get; set; } = 1.0;

        [JsonPropertyName("speedMultiplier")]
        public double SpeedMultiplier { get; set; } = 1.0;
        [JsonPropertyName("effect")]
        public Effect Effect { get; set; } = Effect.None;
    }
    [Flags]
    public enum Effect
    {
        None = 0,
        Radio = 1,
    }
}
