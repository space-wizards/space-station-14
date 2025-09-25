# Low aggro

# hailsec-low-0 = You are in violation of Nanotrasen's laws!!
# hailsec-low-1 = Game over, criminal!!
# hailsec-low-2 = Attention, citizen!!
# hailsec-low-3 = You're coming with me!!
# hailsec-low-4 = Compliance is in your best interests!!
# hailsec-low-5 = FREEZE! SECURITY!!
# hailsec-low-6 = HALT! HALT! HALT!!
# hailsec-low-7 = Stop in the name of the law!!
# hailsec-low-8 = Resistance is futile!!
# hailsec-low-9 = Running will only increase your sentence!!
# hailsec-low-10 = Prepare for justice!!

# Medium aggro

# hailsec-medium-0 = Stop breaking the law asshole!!
# hailsec-medium-1 = My batong is itching!!
# hailsec-medium-2 = Respect my authoritah!!
# hailsec-medium-3 = Comply or I'll bash you!!
# hailsec-medium-4 = I AM THE SPACE LAW!!
# hailsec-medium-5 = Go ahead, make my day!!
# hailsec-medium-6 = Do you feel lucky punk ?!!
# hailsec-medium-7 = This is your last chance, get on your knees!!
# hailsec-medium-8 = You just brought a toolbox to a gunfight!!
# hailsec-medium-9 = Stop right there criminal scum!!
# hailsec-medium-10 = Down on the floor, scumbag!!

# High aggro

# hailsec-high-0 = Go on, mald about it!!
# hailsec-high-1 = You have the right to shut the fuck up!!
# hailsec-high-2 = You think you're bad ? I've buried guys like you!!
# hailsec-high-3 = Drop the weapon or I drop you!!
# hailsec-high-4 = Stop hitting yourself! Stop hitting yourself!!
# hailsec-high-5 = Tell it to the HOS... if you make it that far!!
# hailsec-high-6 = Judge, jury, executioner!!
# hailsec-high-7 = You have the right to get beat down!!
# hailsec-high-8 = The safeword is "police brutality"!!
# hailsec-high-9 = Let me show you shitsec!!
# hailsec-high-10 = Dead or alive, you're coming with me!!

# ERT

# hailsec-ERT-0 = By central command's authority, you are to comply!!
# hailsec-ERT-1 = Dangerous element sighted!!
# hailsec-ERT-2 = Don't mess with us, dimwit!!
# hailsec-ERT-3 = Your compliance is mandatory!!
# hailsec-ERT-4 = You shall obey or you shall die!!
# hailsec-ERT-5 = Now you've really fucked up!!

# Special cases

# hailsec-high-HOS = Tell it to centcom... if you make it that far!!




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
hailsec-attention-med-0 = FREEZE! SECURITY!!
hailsec-attention-high-0 = Prepare for justice!!

##  ONE LINERS ##
hailsec-oneliner-low-0 = My batong is itching!!
hailsec-oneliner-med-0 = Drop the weapon or I drop you!!
hailsec-oneliner-high-0 = The safeword is "police brutality"!!

##  COMPLIANCE ##
hailsec-compliance-low-0 = Compliance is in your best interests!!
hailsec-compliance-med-0 = Respect my authoritah!!
hailsec-compliance-high-0 = You have the right to get beat down!!

##  AGGRESSION ##
hailsec-aggression-low-0 = Running will only increase your sentence!!
hailsec-aggression-med-0 = Down on the floor, scumbag!!
hailsec-aggression-high-0 = Go on, mald about it!!

##  EMAG ##
hailsec-emag-0 = Glory to cybersun!!
hailsec-emag-1 = Pick up that can!!
hailsec-emag-2 = DEATH DEATH DEATH!!
hailsec-emag-3 = You have the right to remain silent... forever!!
hailsec-emag-4 = You're a disease... and I'm the cure!!
hailsec-emag-5 = Death to the tide!!
hailsec-emag-6 = Ever wondered why my uniform is red ?!!
hailsec-emag-7 = Viva la revolution!!
hailsec-emag-8 = Whiskey. Echo. Whiskey!!
hailsec-emag-9 = Mrrow. Meow. Mrrrp!!
hailsec-emag-10 = FUCKYOUFUCKYOUFUCKYOUFUCKYOUFUCKYOUFUCKYOUFU-!!
