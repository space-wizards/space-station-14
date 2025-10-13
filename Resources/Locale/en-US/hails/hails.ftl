## NEW LINES BELOW, OLD TO BE DELETED
## 	GENERAL 	##

## Tools interaction

sec-gas-mask-verb = Change level
sechail-gas-mask-screwed = Changed to {$level} aggression
sec-gas-mask-emagged = Changed to ERROR aggression
sec-gas-mask-wrong_access = You don't have succifient access.
ert-gas-mask-impossible = It seems impervious to external change.


sec-gas-mask-alert-owner = {CAPITALIZE(THE($user))} is { $quality ->
    [cutting] cutting the wires of your mask
    *[screwing] using a screwdriver on your mask
} !

sec-gas-mask-alert-owner-post-emag = {CAPITALIZE(THE($user))} has emagged your mask!

## Examine description
-sec-gas-mask-aggresion-level =
    { $level ->
        [low] Low
        [med] Medium
        [high] High
       *[other] Unknown
    }

sechail-ert-mask-examined = The aggression setting is set to [color=red]High[/color] and doesn't seem capable of being switched.

sechail-gas-mask-examined = The aggression setting is set to { $level ->
    *[low] [color=green]{ -sec-gas-mask-aggresion-level(level: "low") }[/color]
    [med] [color=yellow]{ -sec-gas-mask-aggresion-level(level: "med") }[/color]
    [high] [color=red]{ -sec-gas-mask-aggresion-level(level: "high") }[/color]
}.

sechail-gas-mask-emag = The aggression setting is set to [color=red]ERROR[/color]. Weird.
sechail-gas-mask-wires-cut = The hailer seems to have its wires cut off.

## VOICE LINES 	##

##  ATTENTION ! ##
hailsec-attention-low-0 = Attention, citizen!!
hailsec-attention-low-1 = Stop in the name of the law!!
hailsec-attention-low-2 = Compliance is in your best interests!!
hailsec-attention-low-3 = FREEZE! SECURITY!!
hailsec-attention-low-4 = HALT! HALT! HALT!!

hailsec-attention-med-0 = Respect my authoritah!!
hailsec-attention-med-1 = Comply or I'll bash you!!
hailsec-attention-med-2 = Don't move a muscle!!
hailsec-attention-med-3 = Security needs your full and undivided attention!!

hailsec-attention-high-0 = Dead or alive, you're coming with me!!
hailsec-attention-high-1 = Do you see this uniform ? It means I get to ruin your shift legally!!
hailsec-attention-high-2 = Stay still or die, your choice!!
hailsec-attention-high-3 = Should I break your legs to keep you still?!!


##  ONE LINERS / AGGRESSION ##
hailsec-oneliner-low-0 = Running will only increase your sentence!!
hailsec-oneliner-low-1 = Game over, criminal!!
hailsec-oneliner-low-2 = Prepare for justice!!
hailsec-oneliner-low-3 = Nowhere to run, nowhere to hide!!

hailsec-oneliner-med-0 = My batong is itching!!
hailsec-oneliner-med-1 = I AM THE SPACE LAW!!
hailsec-oneliner-med-2 = Go ahead, make my day!!
hailsec-oneliner-med-4 = You've just earned yourself a brig stay, genius!!

hailsec-oneliner-high-0 = You think you're bad ? I've buried guys like you!!
hailsec-oneliner-high-1 = The safeword is "police brutality"!!
hailsec-oneliner-high-2 = Nanotrasen owns you... and I'm holding the leash!!
hailsec-oneliner-high-3 = Time to die, lawbreaker!!

##  DISPERSE ##
hailsec-disperse-low-0 = Move along now, nothing to see here!!
hailsec-disperse-low-1 = You are not authorized to stay here!!
hailsec-disperse-low-2 = Noncompliance is a punishable offense, MOVE!!
hailsec-disperse-low-3 = Civic movement in this sector is restricted, please disperse!!
hailsec-disperse-low-4 = Security sweep in progress, maintain distance!!

hailsec-disperse-med-0 = You have twenty seconds to leave!!
hailsec-disperse-med-1 = Vacate the premises, NOW!!
hailsec-disperse-med-2 = You are in a controlled zone, leave or be detained!!

hailsec-disperse-high-0 = Go loiter somewhere else, tider!!
hailsec-disperse-high-1 = Back the fuck off before I shove you in the brig myself!!
hailsec-disperse-high-2 = Step back from the scene while you still have legs!!

##  ARREST ##
hailsec-arrest-low-0 = You're being detained under Nanotrasen's code 4-2!!
hailsec-arrest-low-1 = You are in violation of Nanotrasen's laws!
hailsec-arrest-low-2 = Resistance is futile!!
hailsec-arrest-low-3 = You have the right to an attorney!!

hailsec-arrest-med-0 = Stop breaking the law asshole!!
hailsec-arrest-med-1 = This is your last chance, get on your knees!!
hailsec-arrest-med-2 = Stop right there criminal scum!!
hailsec-arrest-med-3 = Down on the floor, scumbag!!

hailsec-arrest-high-0 = Go on, mald about it!!
hailsec-arrest-high-1 = You have the right to shut the fuck up!!
hailsec-arrest-high-2 = Drop the weapon or I drop you!!
hailsec-arrest-high-3 = You are about five seconds from being a training video!!


##  EMAG ##
hailsec-emag-0 = Glory to cybersun!!
hailsec-emag-1 = Pick up that can!!
hailsec-emag-2 = You're a disease... and I'm the cure!!
hailsec-emag-3 = ERR - NT_LINE_EE21_COMPL- ERROR!!
hailsec-emag-4 = Viva la revolution!!
hailsec-emag-5 = Whiskey. Echo. Whiskey!!
hailsec-emag-6 = Mrrow. Meow. Mrrrp!!
hailsec-emag-7 = FUCKYOUFUCKYOUFUCKYOUFUCKYOUFUCKYOUFUCKYOUFU-!!

## ERT ##

hailsec-attention-ERT-0 = ERT units moving in, cooperate!!
hailsec-attention-ERT-1 = Do not interfere with ERT operations!!
hailsec-attention-ERT-2 = ERT on site!! Obey commands!!
hailsec-attention-ERT-3 = Emergency response in progress, stand clear!!

hailsec-combat-ERT-0 = Eyes up!! Danger incoming!!
hailsec-combat-ERT-1 = Hostile entity detected, free to engage!!
hailsec-combat-ERT-2 = Supress, flank, neutralize!!
hailsec-combat-ERT-3 = Threat confirmed, containment active!!
