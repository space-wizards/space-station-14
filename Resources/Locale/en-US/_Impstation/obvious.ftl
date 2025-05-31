obvious-on-item = When {$used}, { SUBJECT($me) } will be [color=white]obvious[/color] to casual examination.
obvious-on-item-currently = { CAPITALIZE(SUBJECT($me)) } { CONJUGATE-BE($me) } [color=white]obvious[/color] to casual examination.
obvious-on-item-for-others = [italic][color=#777777]Others {$will} see:[/color] "{$output}"[/italic]

obvious-reveal-default = worn
obvious-reveal-pockets = worn or pocketed

obvious-on-item-contra-Syndicate = Might make you seem [color=#ff0000]evil[/color].
obvious-on-item-contra-Magical = Might make you seem [color=blue]mystical[/color].
obvious-on-item-contra-Major = Don't be surprised if [color=#cb0000]some people[/color] aren't a fan.

obvious-prefix-default = { CAPITALIZE(SUBJECT($user)) } { CONJUGATE-HAVE($user) }
obvious-prefix-wearing = { CAPITALIZE(SUBJECT($user)) } { CONJUGATE-BE($user) } wearing

## general descriptions
obvious-desc-default = { INDEFINITE($short-type) } { $type } { $thing }.
obvious-desc-colors = { INDEFINITE($thing) } { $thing } in { $type } colors.
obvious-desc-proves = { INDEFINITE($thing) } [color=white]{ $thing }[/color] that proves { SUBJECT($user) } { CONJUGATE-BE($user) } { INDEFINITE($short-type) } { $type }.

## item categories
obvious-thing-default = [italic]something-or-another[/italic]
obvious-thing-cloak = cloak
obvious-thing-pin = pin
obvious-thing-scarf = scarf
obvious-thing-badge = badge
obvious-thing-armband = armband
obvious-thing-medal = medal

## item types
obvious-type-default = [italic]nothing[/italic]

# prides
obvious-type-pride = [color=#d479d4]LGBTQ[/color] [color=white]pride[/color]
obvious-type-pride-aro = [color=#3DA542]aromantic[/color] [color=white]pride[/color]
obvious-type-pride-ace = [color=#efefef]asexual[/color] [color=#800080]pride[/color]
obvious-type-pride-aroace = [color=#efc337]aroace[/color] [color=#45bcee]pride[/color]
obvious-type-pride-lesbian = [color=#EF7627]lesbian[/color] [color=#B55690]pride[/color]
obvious-type-pride-bi = [color=#D60270]bisexual[/color] [color=#0038A8]pride[/color]
obvious-type-pride-pan = [color=#FF218C]pansexual[/color] [color=#21B1FF]pride[/color]
obvious-type-pride-gay = [color=#078D70]gay[/color] [color=#5049CC]pride[/color]
obvious-type-pride-omni = [color=#FE9ACE]omnisexual[/color] [color=8EA6FF]pride[/color]
obvious-type-pride-bear = [color=#FDDC62]bear[/color] [color=#613704]pride[/color]
obvious-type-pride-intersex = [color=#FFD800]intersex[/color] [color=#7902AA]pride[/color]
obvious-type-pride-nonbinary = [color=#FCF434]nonbinary[/color] [color=#9C59D1]pride[/color]
obvious-type-pride-trans = [color=#5BCEFA]transgender[/color] [color=#F5A9B8]pride[/color]
obvious-type-pride-gf = [color=#FF76A4]genderfluid[/color] [color=#2F3CBE]pride[/color]
obvious-type-pride-aut = [color=#FFD700]autism[/color] [color=white]pride[/color]
obvious-type-pride-straightally = [color=#7c7c7c]straight[/color] [color=#d479d4]LGBTQ ally[/color]
obvious-type-pride-gq = [color=#B57EDC]genderqueer[/color] [color=#4A8123]pride[/color]

# lawyers
obvious-type-law = [color=white]certified[/color] [color=#FFD700]lawyer[/color]
obvious-type-law-defense = [color=white]certified[/color] [color=#00C0C0]defense attorney[/color]
obvious-type-law-prosecution = [color=white]certified[/color] [color=#FF2222]prosecuting attorney[/color]

## SPECIFIC DESCRIPTIONS (skips the above description-builders)
obvious-x-scarf-lesbian-long = some [color=#EF7627]long[/color] [color=white]ba[/color][color=#B55690]con[/color].
obvious-x-medal-nothing = a [color=#FFD700]gleaming medal[/color]!
# medals should be in the description building functions but there's too many of them and this feature is already over-scope

# imp only items
obvious-x-pin-straight = a [color=#7c7c7c]straight[/color] [color=white]pride[/color] pin... [italic]Ew.[/italic]
obvious-x-cloak-straight = a cloak in the [color=#7c7c7c]straight[/color] [color=white]pride[/color] colors... [italic]Ew.[/italic]

