## 	GENERAL 	##

## Tools interaction

hailer-gas-mask-verb = Change level
hailer-gas-mask-screwed = Changed to {$level} aggression
hailer-gas-mask-emagged = Changed to ERROR aggression
hailer-gas-mask-wrong_access = You don't have succifient access.

## Examine description
-sec-gas-mask-aggresion-level =
    { $level ->
        [low] Low
        [med] Medium
        [high] High
       *[other] Unknown
    }

hailer-ert-mask-examined = The aggression setting is set to [color=red]High[/color] and doesn't seem capable of being switched.

hailer-gas-mask-examined = The aggression setting is set to { $level ->
    *[low] [color=green]{ -sec-gas-mask-aggresion-level(level: "low") }[/color]
    [med] [color=yellow]{ -sec-gas-mask-aggresion-level(level: "med") }[/color]
    [high] [color=red]{ -sec-gas-mask-aggresion-level(level: "high") }[/color]
}.

hailer-gas-mask-emag = The aggression setting is set to [color=red]ERROR[/color]. Weird.
hailer-gas-mask-wires-cut = The hailer seems to have its wires cut off.

## VOICE LINES 	##

##  ATTENTION ! ##
hailer-attention-low-0 = Attention, citizen!
hailer-attention-low-1 = Stop in the name of the law!
hailer-attention-low-2 = Compliance is in your best interests!
hailer-attention-low-3 = FREEZE! SECURITY!
hailer-attention-low-4 = HALT! HALT! HALT!

hailer-attention-med-0 = Respect my authoritah!
hailer-attention-med-1 = Comply or I'll bash you!
hailer-attention-med-2 = Don't move a muscle!
hailer-attention-med-3 = Security needs your full and undivided attention!

hailer-attention-high-0 = Dead or alive, you're coming with me!
hailer-attention-high-1 = Do you see this uniform?! It means I get to ruin your shift legally!
hailer-attention-high-2 = Stay still or die, your choice!
hailer-attention-high-3 = Should I break your legs to keep you still?!


##  ONE LINERS / AGGRESSION ##
hailer-oneliner-low-0 = Running will only increase your sentence!
hailer-oneliner-low-1 = Game over, criminal!
hailer-oneliner-low-2 = Prepare for justice!
hailer-oneliner-low-3 = Nowhere to run, nowhere to hide!

hailer-oneliner-med-0 = My batong is itching!
hailer-oneliner-med-1 = I AM THE SPACE LAW!
hailer-oneliner-med-2 = Go ahead, make my day!
hailer-oneliner-med-3 = You've just earned yourself a brig stay, genius!

hailer-oneliner-high-0 = You think you're bad?! I've buried guys like you!
hailer-oneliner-high-1 = The safeword is "police brutality"!
hailer-oneliner-high-2 = Nanotrasen owns you... and I'm holding the leash!
hailer-oneliner-high-3 = Time to die, lawbreaker!

##  DISPERSE ##
hailer-disperse-low-0 = Move along now, nothing to see here!
hailer-disperse-low-1 = You are not authorized to stay here!
hailer-disperse-low-2 = Noncompliance is a punishable offense, MOVE!
hailer-disperse-low-3 = Civic movement in this sector is restricted, please disperse!
hailer-disperse-low-4 = Security sweep in progress, maintain distance!

hailer-disperse-med-0 = You have twenty seconds to leave!
hailer-disperse-med-1 = Vacate the premises, NOW!
hailer-disperse-med-2 = You are in a controlled zone, leave or be detained!

hailer-disperse-high-0 = Go loiter somewhere else, tider!
hailer-disperse-high-1 = Back the fuck off before I shove you in the brig myself!
hailer-disperse-high-2 = Step back from the scene while you still have legs!

##  ARREST ##
hailer-arrest-low-0 = You are being detained under Nanotrasen's code 4-2!
hailer-arrest-low-1 = You are in violation of Nanotrasen's laws!
hailer-arrest-low-2 = Resistance is futile!
hailer-arrest-low-3 = You have the right to an attorney!

hailer-arrest-med-0 = Stop breaking the law asshole!
hailer-arrest-med-1 = This is your last chance, get on your knees!
hailer-arrest-med-2 = Stop right there criminal scum!
hailer-arrest-med-3 = Down on the floor, scumbag!

hailer-arrest-high-0 = Go on, mald about it!
hailer-arrest-high-1 = You have the right to shut the fuck up!
hailer-arrest-high-2 = Drop the weapon or I drop you!
hailer-arrest-high-3 = You are about five seconds from becoming a training video!


##  EMAG ##
hailer-emag-0 = Glory to cybersun!
hailer-emag-1 = Pick up that can!
hailer-emag-2 = You're a disease... and I'm the cure!
hailer-emag-3 = Viva la revolution!
hailer-emag-4 = Whiskey. Echo. Whiskey!
hailer-emag-5 = Mrrow. Meow. Mrrrp!
hailer-emag-6 = FUCKYOUFUCKYOUFUCKYOUFUCKYOUFUCKYOUFUCKYOUFU-

## ERT ##

hailer-ERT-attention-0 = Emergency Response Team present, cooperate!
hailer-ERT-attention-1 = Do not interfere with ERT operations!
hailer-ERT-attention-2 = ERT on site! Obey commands!
hailer-ERT-attention-3 = Emergency response in progress, do not resist!

hailer-ERT-combat-0 = Eyes up! Danger incoming!
hailer-ERT-combat-1 = Hostile detected!
hailer-ERT-combat-2 = The price for insubordination is death!
hailer-ERT-combat-3 = You are facing Centcom's finest!
