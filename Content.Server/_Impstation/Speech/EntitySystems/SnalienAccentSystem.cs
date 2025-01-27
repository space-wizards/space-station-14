using System.Text.RegularExpressions;
using Content.Server.Speech.Components;

namespace Content.Server.Speech.EntitySystems;

public sealed class SnalienAccentSystem : EntitySystem
{
    private static readonly Regex RegexLowerslurw = new Regex("w{1,3}");
    private static readonly Regex RegexUpperslurw = new Regex("W{1,3}");
    private static readonly Regex RegexLowerslure = new Regex("e{1,3}");
    private static readonly Regex RegexUpperslure = new Regex("E{1,3}");
	private static readonly Regex RegexLowerslurr = new Regex("r{1,3}");
    private static readonly Regex RegexUpperslurr = new Regex("R{1,3}");
    private static readonly Regex RegexLowerslury = new Regex("y{1,3}");
    private static readonly Regex RegexUpperslury = new Regex("Y{1,3}");
	private static readonly Regex RegexLowersluru = new Regex("u{1,3}");
    private static readonly Regex RegexUppersluru = new Regex("U{1,3}");
    private static readonly Regex RegexLowersluri = new Regex("i{1,3}");
    private static readonly Regex RegexUppersluri = new Regex("I{1,3}");
	private static readonly Regex RegexLowersluro = new Regex("o{1,3}");
    private static readonly Regex RegexUppersluro = new Regex("O{1,3}");
    private static readonly Regex RegexLowerslura = new Regex("a{1,3}");
    private static readonly Regex RegexUpperslura = new Regex("A{1,3}");
	private static readonly Regex RegexLowerslurs = new Regex("s{1,3}");
    private static readonly Regex RegexUpperslurs = new Regex("S{1,3}");
    private static readonly Regex RegexLowerslurf = new Regex("f{1,3}");
    private static readonly Regex RegexUpperslurf = new Regex("F{1,3}");
	private static readonly Regex RegexLowerslurh = new Regex("h{1,3}");
    private static readonly Regex RegexUpperslurh = new Regex("H{1,3}");
    private static readonly Regex RegexLowerslurl = new Regex("l{1,3}");
    private static readonly Regex RegexUpperslurl = new Regex("L{1,3}");
	private static readonly Regex RegexLowerslurz = new Regex("z{1,3}");
    private static readonly Regex RegexUpperslurz = new Regex("Z{1,3}");
    private static readonly Regex RegexLowerslurv = new Regex("v{1,3}");
    private static readonly Regex RegexUpperslurv = new Regex("V{1,3}");
	private static readonly Regex RegexLowerslurn = new Regex("n{1,3}");
    private static readonly Regex RegexUpperslurn = new Regex("N{1,3}");
    private static readonly Regex RegexLowerslurm = new Regex("m{1,3}");
    private static readonly Regex RegexUpperslurm = new Regex("M{1,3}");
	private static readonly Regex RegexLowerAnd = new Regex("aannd ");
	private static readonly Regex RegexLowerFor = new Regex("ffoorr ");
	private static readonly Regex RegexLowerBut = new Regex("buut ");
	private static readonly Regex RegexLowerOr = new Regex("oorr ");
	private static readonly Regex RegexLowerSo = new Regex("ssoo ");
	private static readonly Regex RegexLowerYet = new Regex("yyeet ");
	private static readonly Regex RegexLowerAs = new Regex("aass ");
	private static readonly Regex RegexLowerAfter = new Regex("aaffteerr ");
	private static readonly Regex RegexLowerThough = new Regex("thhoouughh ");
	private static readonly Regex RegexLowerBecause = new Regex("beecaauussee ");
	private static readonly Regex RegexLowerIn = new Regex("iinn ");
	private static readonly Regex RegexLowerOnce = new Regex("oonncee ");
	private static readonly Regex RegexLowerSince = new Regex("ssiinncee ");
	private static readonly Regex RegexLowerWhen = new Regex("wwhheenn ");
	private static readonly Regex RegexLowerWhere = new Regex("wwhheerree ");
	private static readonly Regex RegexLowerWhile = new Regex("wwhhiillee ");
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SnalienAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, SnalienAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        // I see nor hear no evil
        message = RegexLowerslurw.Replace(message, "ww");
        // Black writings on the wall
        message = RegexUpperslurw.Replace(message, "WW");
        // Unleashed a million faces
        message = RegexLowerslure.Replace(message, "ee");
        // And one by one they fall
        message = RegexUpperslure.Replace(message, "EE");
        // Black hearted evil
        message = RegexLowerslurr.Replace(message, "rr");
        // Black hearted hero
        message = RegexUpperslurr.Replace(message, "RR");
        // I am all
        message = RegexLowerslury.Replace(message, "yy");
        // I am all
        message = RegexUpperslury.Replace(message, "YY");
        // I am
        message = RegexLowersluru.Replace(message, "uu");
        // Here we go, buddy
        message = RegexUppersluru.Replace(message, "UU");
        // Here we go, buddy
        message = RegexLowersluri.Replace(message, "ii");
        // Here we go
        message = RegexUppersluri.Replace(message, "II");
        // Here we go, buddy
        message = RegexLowersluro.Replace(message, "oo");
        // Here we go
        message = RegexUppersluro.Replace(message, "OO");
        // Go ahead and try to see through me
        message = RegexLowerslura.Replace(message, "aa");
        // Do it if you dare
        message = RegexUpperslura.Replace(message, "AA");
        // One step forwards, two steps back
        message = RegexLowerslurs.Replace(message, "ss");
        // I'm here
        message = RegexUpperslurs.Replace(message, "SS");
        // Do it
        message = RegexLowerslurf.Replace(message, "ff");
        // Do it
        message = RegexUpperslurf.Replace(message, "FF");
        // Do it
        message = RegexLowerslurh.Replace(message, "hh");
        // Do it
        message = RegexUpperslurh.Replace(message, "HH");
        // Can you see all of me
        message = RegexLowerslurl.Replace(message, "ll");
        // Walk into my mystery
        message = RegexUpperslurl.Replace(message, "LL");
        // Step inside and hold on for dear life
        message = RegexLowerslurz.Replace(message, "zz");
        // Do you remember me
        message = RegexUpperslurz.Replace(message, "ZZ");
        // Capture you or set you free
        message = RegexLowerslurv.Replace(message, "vv");
        // I am all
        message = RegexUpperslurv.Replace(message, "VV");
        // I am all of me
        message = RegexLowerslurn.Replace(message, "nn");
        // I am
        message = RegexUpperslurn.Replace(message, "NN");
        // I am
        message = RegexLowerslurm.Replace(message, "mm");
        // I am
        message = RegexUpperslurm.Replace(message, "MM");
		// Begin Beck's Contributions
		// What am I supposed to do if I want to talk about peace and understanding
		message = RegexLowerAnd.Replace(message, "aannd... ");
		// But you only understand the language of the sword
		message = RegexLowerFor.Replace(message, "ffoorr... ");
		// What if I want to make you understand that the path you chose leads to downfall
		message = RegexLowerBut.Replace(message, "buut... ");
		// But you only understand the language of the sword
		message = RegexLowerOr.Replace(message, "oorr... ");
		// What if I want to tell you to leave me and my beloved ones in peace
		message = RegexLowerSo.Replace(message, "ssoo... ");
		// But you only understand the language of the sword
		message = RegexLowerYet.Replace(message, "yyeet... ");
		// I let the blade do the talking
		message = RegexLowerAs.Replace(message, "aass... ");
		// So my tongue shall become iron
		message = RegexLowerAfter.Replace(message, "aaffteerr... ");
		// And my words the mighty roar of war
		message = RegexLowerThough.Replace(message, "thhoouughh... ");
		// Revealing my divine anger's arrow shall strike
		message = RegexLowerBecause.Replace(message, "beecaauussee... ");
		// All action for the good of all
		message = RegexLowerIn.Replace(message, "iinn... ");
		// I see my reflection in your eyes
		message = RegexLowerOnce.Replace(message, "oonncee... ");
		// But my new age has just begun
		message = RegexLowerSince.Replace(message, "ssiinncee... ");
		// The sword is soft in the fire of the furnace
		message = RegexLowerWhen.Replace(message, "wwhheenn... ");
		// It hungers to be hit, and wants to have a hundred sisters
		message = RegexLowerWhere.Replace(message, "wwhheerree... ");
		// In the coldest state of their existence
		message = RegexLowerWhile.Replace(message, "wwhhiillee... ");
        args.Message = message;
    }
}