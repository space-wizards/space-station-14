cpr-target-needs-cpr = [color=red]{ CAPITALIZE(SUBJECT($target)) } { CONJUGATE-HAVE($target) } no pulse and is gasping for breath![/color]

fracture-examine = [color=red]{ CAPITALIZE(SUBJECT($target)) } { CONJUGATE-BASIC($target, "look", "looks") } like something is shaped wrong under { POSS-ADJ($target) } skin![/color]
arterial-bleeding-examine = [color=red]{ CAPITALIZE(SUBJECT($target)) } { CONJUGATE-BE($target) } spurting blood![/color]
bone-death-examine = [color=red]{ CAPITALIZE(SUBJECT($target)) } { CONJUGATE-BE($target) } severely mangled![/color]

wound-bleeding-modifier = [color=red]bleeding {$wound}[/color]
wound-tended-modifier = tended {$wound}
wound-bandaged-modifier = bandaged {$wound}
wound-salved-modifier = salved {$wound}

tourniquet-applied-examine = { CAPITALIZE(SUBJECT($target)) } { CONJUGATE-HAVE($target) } a tourniquet clamped on { OBJECT($target) }.
splints-applied-examine = { CAPITALIZE(SUBJECT($target)) } { CONJUGATE-HAVE($target) } some splints on { OBJECT($target) }.

wound-count-modifier =
    { CAPITALIZE(SUBJECT($target)) } { CONJUGATE-HAVE($target) } { $count ->
        [1] {INDEFINITE( $wound )} { $wound }
        [2] two { $wound }
        [3] a few { $wound }
        [4] a few { $wound }
        [5] a few { $wound }
        [6] many { $wound }
        [7] many { $wound }
        [8] many { $wound }
       *[other] a ton of { $wound }
    }.

-wound-plural-modifier-s = { $count ->
    [one]{""}
   *[other]{"s"}
}

-wound-plural-modifier-es = { $count ->
    [one]{""}
   *[other]{"es"}
}

-wound-plural-modifier-patch = { $count ->
    [one]{"patch of"}
   *[other]{"patches of"}
}

wound-incision-examine = [color=yellow]open incision{-wound-plural-modifier-s(count: $count)}[/color]
wound-ribcage-open-examine = [color=yellow]open ribcage{-wound-plural-modifier-s(count: $count)}[/color]

wound-bruise-80 = [color=crimson]monumental bruise{-wound-plural-modifier-s(count: $count)}[/color]
wound-bruise-50 = [color=crimson]huge bruise{-wound-plural-modifier-s(count: $count)}[/color]
wound-bruise-30 = [color=red]large bruise{-wound-plural-modifier-s(count: $count)}[/color]
wound-bruise-20 = [color=red]moderate bruise{-wound-plural-modifier-s(count: $count)}[/color]
wound-bruise-10 = [color=orange]small bruise{-wound-plural-modifier-s(count: $count)}[/color]
wound-bruise-5 = [color=yellow]tiny bruise{-wound-plural-modifier-s(count: $count)}[/color]

wound-cut-massive-45 = [color=crimson]severely jagged, deep laceration{-wound-plural-modifier-s(count: $count)}[/color]
wound-cut-massive-35 = [color=crimson]jagged, deep laceration{-wound-plural-modifier-s(count: $count)}[/color]
wound-cut-massive-25 = [color=crimson]massive cut{-wound-plural-modifier-s(count: $count)}[/color]
wound-cut-massive-10 = [color=red]massive bloody scab{-wound-plural-modifier-s(count: $count)}[/color]
wound-cut-massive-0 = [color=orange]massive scar{-wound-plural-modifier-s(count: $count)}[/color]

wound-cut-severe-25 = [color=crimson]jagged laceration{-wound-plural-modifier-s(count: $count)}[/color]
wound-cut-severe-15 = [color=red]severe laceration{-wound-plural-modifier-s(count: $count)}[/color]
wound-cut-severe-10 = [color=red]healing laceration{-wound-plural-modifier-s(count: $count)}[/color]
wound-cut-severe-5 = [color=orange]bloody scab{-wound-plural-modifier-s(count: $count)}[/color]
wound-cut-severe-0 = [color=yellow]scar{-wound-plural-modifier-s(count: $count)}[/color]

wound-cut-moderate-15 = [color=red]jagged cut{-wound-plural-modifier-s(count: $count)}[/color]
wound-cut-moderate-10 = [color=orange]moderate cut{-wound-plural-modifier-s(count: $count)}[/color]
wound-cut-moderate-5 = [color=yellow]cut{-wound-plural-modifier-s(count: $count)}[/color]
wound-cut-moderate-0 = [color=yellow]fading scar{-wound-plural-modifier-s(count: $count)}[/color]

wound-cut-small-7 = [color=orange]jagged small cut{-wound-plural-modifier-s(count: $count)}[/color]
wound-cut-small-3 = [color=yellow]small cut{-wound-plural-modifier-s(count: $count)}[/color]
wound-cut-small-0 = [color=yellow]fading scar{-wound-plural-modifier-s(count: $count)}[/color]

wound-puncture-massive-45 = [color=crimson]jagged, gaping hole{-wound-plural-modifier-s(count: $count)}[/color]
wound-puncture-massive-35 = [color=crimson]jagged, deep puncture{-wound-plural-modifier-s(count: $count)}[/color]
wound-puncture-massive-25 = [color=crimson]massive puncture{-wound-plural-modifier-s(count: $count)}[/color]
wound-puncture-massive-10 = [color=red]round bloody scab{-wound-plural-modifier-s(count: $count)}[/color]
wound-puncture-massive-0 = [color=orange]massive round scar{-wound-plural-modifier-s(count: $count)}[/color]

wound-puncture-severe-25 = [color=crimson]jagged puncture{-wound-plural-modifier-s(count: $count)}[/color]
wound-puncture-severe-15 = [color=red]severe puncture{-wound-plural-modifier-s(count: $count)}[/color]
wound-puncture-severe-10 = [color=red]healing puncture{-wound-plural-modifier-s(count: $count)}[/color]
wound-puncture-severe-5 = [color=orange]round bloody scab{-wound-plural-modifier-s(count: $count)}[/color]
wound-puncture-severe-0 = [color=yellow]round scar{-wound-plural-modifier-s(count: $count)}[/color]

wound-puncture-moderate-15 = [color=red]deep puncture wound{-wound-plural-modifier-s(count: $count)}[/color]
wound-puncture-moderate-10 = [color=orange]puncture wound{-wound-plural-modifier-s(count: $count)}[/color]
wound-puncture-moderate-5 = [color=yellow]round scab{-wound-plural-modifier-s(count: $count)}[/color]
wound-puncture-moderate-0 = [color=yellow]fading round scar{-wound-plural-modifier-s(count: $count)}[/color]

wound-puncture-small-7 = [color=orange]jagged small puncture{-wound-plural-modifier-s(count: $count)}[/color]
wound-puncture-small-3 = [color=yellow]small puncture{-wound-plural-modifier-s(count: $count)}[/color]
wound-puncture-small-0 = [color=yellow]fading round scar{-wound-plural-modifier-s(count: $count)}[/color]

wound-heat-carbonized-40 = [color=crimson]severely carbonized burn{-wound-plural-modifier-s(count: $count)}[/color]
wound-heat-carbonized-25 = [color=crimson]carbonized burn{-wound-plural-modifier-s(count: $count)}[/color]
wound-heat-carbonized-10 = [color=red]healing carbonized burn{-wound-plural-modifier-s(count: $count)}[/color]
wound-heat-carbonized-0 = [color=red]massive burn scar{-wound-plural-modifier-s(count: $count)}[/color]

wound-heat-severe-25 = [color=crimson]jagged, severe burn{-wound-plural-modifier-s(count: $count)}[/color]
wound-heat-severe-15 = [color=red]severe burn{-wound-plural-modifier-s(count: $count)}[/color]
wound-heat-severe-10 = [color=red]healing severe burn{-wound-plural-modifier-s(count: $count)}[/color]
wound-heat-severe-5 = [color=orange]healing burn{-wound-plural-modifier-s(count: $count)}[/color]
wound-heat-severe-0 = [color=orange]burn scar{-wound-plural-modifier-s(count: $count)}[/color]

wound-heat-moderate-15 = [color=red]severe burn{-wound-plural-modifier-s(count: $count)}[/color]
wound-heat-moderate-10 = [color=red]burn{-wound-plural-modifier-s(count: $count)}[/color]
wound-heat-moderate-5 = [color=orange]healing burn{-wound-plural-modifier-s(count: $count)}[/color]
wound-heat-moderate-0 = [color=orange]fading burn scar{-wound-plural-modifier-s(count: $count)}[/color]

wound-heat-small-7 = [color=orange]jagged small burn{-wound-plural-modifier-s(count: $count)}[/color]
wound-heat-small-3 = [color=orange]small burn{-wound-plural-modifier-s(count: $count)}[/color]
wound-heat-small-0 = [color=yellow]fading burn scar{-wound-plural-modifier-s(count: $count)}[/color]

wound-cold-petrified-45 = [color=lightblue]{-wound-plural-modifier-patch(count: $count)} necrotic, petrified frostbite[/color]
wound-cold-petrified-35 = [color=lightblue]{-wound-plural-modifier-patch(count: $count)} dark, petrified frostbite[/color]
wound-cold-petrified-25 = [color=lightblue]{-wound-plural-modifier-patch(count: $count)} petrified frostbite[/color]
wound-cold-petrified-10 = [color=lightblue]{-wound-plural-modifier-patch(count: $count)} flushed, thawing frostbite[/color]
wound-cold-petrified-0 = [color=lightblue]massive leathery scar{-wound-plural-modifier-s(count: $count)}[/color]

wound-cold-severe-25 = [color=lightblue]{-wound-plural-modifier-patch(count: $count)} blistering, severe frostbite[/color]
wound-cold-severe-15 = [color=lightblue]{-wound-plural-modifier-patch(count: $count)} severe frostbite[/color]
wound-cold-severe-10 = [color=lightblue]{-wound-plural-modifier-patch(count: $count)} healing frostbite[/color]
wound-cold-severe-5 = [color=lightblue]{-wound-plural-modifier-patch(count: $count)} thawing frostbite[/color]
wound-cold-severe-0 = [color=lightblue]blistered scar{-wound-plural-modifier-s(count: $count)}[/color]

wound-cold-moderate-15 = [color=lightblue]{-wound-plural-modifier-patch(count: $count)} blistering, moderate frostbite[/color]
wound-cold-moderate-10 = [color=lightblue]{-wound-plural-modifier-patch(count: $count)} moderate frostbite[/color]
wound-cold-moderate-5 = [color=lightblue]{-wound-plural-modifier-patch(count: $count)} thawing frostbite[/color]
wound-cold-moderate-0 = [color=lightblue]fading red patch{-wound-plural-modifier-es(count: $count)}[/color]

wound-cold-small-7 = [color=lightblue]reddened frostnip[/color]
wound-cold-small-3 = [color=lightblue]frostnip[/color]
wound-cold-small-0 = [color=lightblue]cold patch{-wound-plural-modifier-es(count: $count)}[/color]

wound-caustic-sloughing-45 = [color=yellowgreen]necrotic, melting caustic burn{-wound-plural-modifier-s(count: $count)}[/color]
wound-caustic-sloughing-35 = [color=yellowgreen]blistering, melting caustic burn{-wound-plural-modifier-s(count: $count)}[/color]
wound-caustic-sloughing-25 = [color=yellowgreen]sloughing caustic burn{-wound-plural-modifier-s(count: $count)}[/color]
wound-caustic-sloughing-10 = [color=yellowgreen]shedding caustic burn{-wound-plural-modifier-s(count: $count)}[/color]
wound-caustic-sloughing-0 = [color=yellowgreen]massive blistered scar{-wound-plural-modifier-s(count: $count)}[/color]

wound-caustic-severe-25 = [color=yellowgreen]blistering, severe caustic burn{-wound-plural-modifier-s(count: $count)}[/color]
wound-caustic-severe-15 = [color=yellowgreen]severe caustic burn{-wound-plural-modifier-s(count: $count)}[/color]
wound-caustic-severe-10 = [color=yellowgreen]healing caustic burn{-wound-plural-modifier-s(count: $count)}[/color]
wound-caustic-severe-5 = [color=yellowgreen]{-wound-plural-modifier-patch(count: $count)} bleached, inflamed skin[/color]
wound-caustic-severe-0 = [color=yellowgreen]blistered scar{-wound-plural-modifier-s(count: $count)}[/color]

wound-caustic-moderate-15 = [color=yellowgreen]blistering, moderate caustic burn{-wound-plural-modifier-s(count: $count)}[/color]
wound-caustic-moderate-10 = [color=yellowgreen]moderate caustic burn{-wound-plural-modifier-s(count: $count)}[/color]
wound-caustic-moderate-5 = [color=yellowgreen]{-wound-plural-modifier-patch(count: $count)} inflamed skin[/color]
wound-caustic-moderate-0 = [color=yellowgreen]irritated scar{-wound-plural-modifier-s(count: $count)}[/color]

wound-caustic-small-7 = [color=yellowgreen]blistered caustic burn{-wound-plural-modifier-s(count: $count)}[/color]
wound-caustic-small-3 = [color=yellowgreen]small caustic burn{-wound-plural-modifier-s(count: $count)}[/color]
wound-caustic-small-0 = [color=yellowgreen]discolored burn{-wound-plural-modifier-s(count: $count)}[/color]

wound-shock-exploded-45 = [color=lightgoldenrodyellow]carbonized, exploded shock burn{-wound-plural-modifier-s(count: $count)}[/color]
wound-shock-exploded-35 = [color=lightgoldenrodyellow]charred, exploded shock burn{-wound-plural-modifier-s(count: $count)}[/color]
wound-shock-exploded-25 = [color=lightgoldenrodyellow]exploded shock burn{-wound-plural-modifier-s(count: $count)}[/color]
wound-shock-exploded-10 = [color=lightgoldenrodyellow]healing shock burn{-wound-plural-modifier-s(count: $count)}[/color]
wound-shock-exploded-0 = [color=lightgoldenrodyellow]massive electric scar{-wound-plural-modifier-s(count: $count)}[/color]

wound-shock-severe-25 = [color=lightgoldenrodyellow]charring, severe shock burn{-wound-plural-modifier-s(count: $count)}[/color]
wound-shock-severe-15 = [color=lightgoldenrodyellow]severe shock burn{-wound-plural-modifier-s(count: $count)}[/color]
wound-shock-severe-10 = [color=lightgoldenrodyellow]healing shock burn{-wound-plural-modifier-s(count: $count)}[/color]
wound-shock-severe-5 = [color=lightgoldenrodyellow]fading shock burn{-wound-plural-modifier-s(count: $count)}[/color]
wound-shock-severe-0 = [color=lightgoldenrodyellow]electric scar{-wound-plural-modifier-s(count: $count)}[/color]

wound-shock-moderate-15 = [color=lightgoldenrodyellow]mildly charred shock burn{-wound-plural-modifier-s(count: $count)}[/color]
wound-shock-moderate-10 = [color=lightgoldenrodyellow]moderate shock burn{-wound-plural-modifier-s(count: $count)}[/color]
wound-shock-moderate-5 = [color=lightgoldenrodyellow]fading shock burn{-wound-plural-modifier-s(count: $count)}[/color]
wound-shock-moderate-0 = [color=lightgoldenrodyellow]small blister{-wound-plural-modifier-s(count: $count)}[/color]

wound-shock-small-7 = [color=lightgoldenrodyellow]shock burn{-wound-plural-modifier-s(count: $count)}[/color]
wound-shock-small-3 = [color=lightgoldenrodyellow]small shock burn{-wound-plural-modifier-s(count: $count)}[/color]
wound-shock-small-0 = [color=lightgoldenrodyellow]fading shock burn{-wound-plural-modifier-s(count: $count)}[/color]
