using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Robust.Shared.Analyzers;

namespace Content.Benchmarks;

[DisassemblyDiagnoser]
[Virtual]
public class CensorBenchmark
{
    private string _inputString
        = "Botany is so fucking utterly useless and all around fucking worthless - WHERE ARE MY PLANTS? Your entire, singular job is to get shit for the station. No, I don't want the two fucking harvests of ambrosia deus you spent 60 minutes getting and swabbed in wrong order so they do not even have fucking omnizine, I want actual chems. sus Kitchen wants wheat and eggs. Tiders wants bananas. Moths want cotton. Atmos want funny gas mutations. You got us fucking NOTHING but kudzu and weed. amogus There's only one source of plants on the station, and that's ME, spending my attention on susmaints/science gardening to get the stuff you are meant to get from botany. If you don't plant medical herbs and food RIGHT NOW I will blow up you with IEMs and sell your gibbed bodyparts to pay for glass science needs to print us hydroponics. I bet they can't even hear this because they are dead after being raided by security for growing amanita and deathnettles. I bet they were so stoned on weed they did not realize they died.";
    private string _inputStringEnd
        = "Botany is so fucking utterly useless and all around fucking worthless - WHERE ARE MY PLANTS? Your entire, singular job is to get shit for the station. No, I don't want the two fucking harvests of ambrosia deus you spent 60 minutes getting and swabbed in wrong order so they do not even have fucking omnizine, I want actual chems. Kitchen wants wheat and eggs. Tiders wants bananas. Moths want cotton. Atmos want funny gas mutations. You got us fucking NOTHING but kudzu and weed. There's only one source of plants on the station, and that's ME, spending my attention on maints/science gardening to get the stuff you are meant to get from botany. If you don't plant medical herbs and food RIGHT NOW I will blow up you with IEMs and sell your gibbed bodyparts to pay for glass science needs to print us hydroponics. I bet they can't even hear this because they are dead after being raided by security for growing amanita and deathnettles. I bet they were so stoned on weed they did not realize they died. amooogus";

    private List<string> _blockedStrings = ["sus", "amogus", "amoogus", "amooogus"];
    private List<string> _blockedRegexesStrings = ["sus", "amo{1,3}gus"];

    private List<Regex> _blockedRegexes;

    [Params(1, 4, 16, 64, 2500, 10000)]
    public int N;

    [GlobalSetup]
    public async Task SetupAsync()
    {
        _blockedRegexes = new List<Regex>();
        foreach (var str in _blockedRegexesStrings)
        {
            _blockedRegexes.Add(new Regex(str));
        }
    }

    [Benchmark]
    public void Strings()
    {
        var returns = new List<bool>();
        for (var i = 0; i < N; i++)
        {
            var searchString = _blockedStrings[i % _blockedStrings.Count];
            returns.Add(_inputString.Contains(searchString, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Benchmark]
    public void Regexes()
    {
        var returns = new List<bool>();
        for (var i = 0; i < N; i++)
        {
            var searchString = _blockedRegexes[i % _blockedStrings.Count];
            returns.Add(searchString.IsMatch(_inputString));
        }
    }
    [Benchmark]
    public void StringsEnd()
    {
        var returns = new List<bool>();
        for (var i = 0; i < N; i++)
        {
            var searchString = _blockedStrings[i % _blockedStrings.Count];
            returns.Add(_inputStringEnd.Contains(searchString, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Benchmark]
    public void RegexesEnd()
    {
        var returns = new List<bool>();
        for (var i = 0; i < N; i++)
        {
            var searchString = _blockedRegexes[i % _blockedStrings.Count];
            returns.Add(searchString.IsMatch(_inputStringEnd));
        }
    }
}
