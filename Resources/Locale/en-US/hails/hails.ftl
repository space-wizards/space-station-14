# Low aggro
# These should be very formal order to comply. Don't mention "arrest" or put any hard insult/swear in it.

hail-low-0 = You are in violation of Nanotrasen's law!!
hail-low-1 = Game over, criminal!!
hail-low-2 = Attention, citizen!!
hail-low-3 = You're coming with me!!
hail-low-4 = Compliance is in your best interests!!
hail-low-5 = FREEZE! SECURITY!!
hail-low-6 = HALT! HALT! HALT!!
hail-low-7 = Stop in the name of the law!!
hail-low-8 = Resistance is futile!!
hail-low-9 = Running will only increase your sentence!!
hail-low-10 = Prepare for justice!!

# Medium aggro
# Dirty Harry territory. More casual, more free on the vulgarity. Cowboy-like attitude.

hail-medium-0 = Stop breaking the law asshole!!
hail-medium-1 = My batong is itching!!
hail-medium-2 = Respect my authoritah!!
hail-medium-3 = Comply or I'll bash you!!
hail-medium-4 = I AM THE SPACE LAW!!
hail-medium-5 = Go ahead, make my day!!
hail-medium-6 = Do you feel lucky punk ?!!
hail-medium-7 = This is your last chance, get on your knees!!
hail-medium-8 = You just brought a toolbox to a gunfight!!
hail-medium-9 = Stop right there criminal scum!!
hail-medium-10 = Down on the floor, scumbag!!

# High aggro
# Real shitsec hours. Insults, threats, etc. Keep in mind this is still coming from a security device and this should reflect this.

hail-high-0 = Go on, mald about it!!
hail-high-1 = You have the right to shut the fuck up!!
hail-high-2 = You think you're bad ? I've buried guys like you!!
hail-high-3 = Drop the weapon or I drop you!!
hail-high-4 = Stop hitting yourself! Stop hitting yourself!!
hail-high-5 = Tell it to the HOS... if you make it that far!!
hail-high-6 = Judge, jury, executioner!!
hail-high-7 = You have the right to get beat down!!
hail-high-8 = The safe word is "police brutality"!!
hail-high-9 = Let me show you shitsec!!
hail-high-10 = Dead or alive, you're coming with me!!

# Emag
# What would make a syndi giggle ? Basically either overboard or a very funny thing.

hail-emag-0 = Glory to cybersun!!
hail-emag-1 = Pick up that can!!
hail-emag-2 = DEATH DEATH DEATH!!
hail-emag-3 = You have the right to remain silent... forever!!
hail-emag-4 = You're a disease... and I'm the cure!!
hail-emag-5 = Death to the tide!!
hail-emag-6 = Ever wondered why my uniform is red ?!!
hail-emag-7 = Viva la revolution!!
hail-emag-8 = Whiskey. Echo. Whiskey!!
hail-emag-9 = Mrrow. Meow. Mrrrp!!
hail-emag-10 = FUCKYOUFUCKYOUFUCKYOUFUCKYOUFUCKYOUFUCKYOUFU-!!

# ERT
# No low, med, high since I don't think they will have time to mess with their setting

hail-ERT-0 = By central command's authority, you are to comply!!
hail-ERT-1 = Dangerous element sighted!!
hail-ERT-2 = Don't mess with us, dimwit!!
hail-ERT-3 = Your compliance is mandatory!!
hail-ERT-4 = You shall obey or you shall die!!
hail-ERT-5 = Now you've really fucked up!!

# Special cases

hail-high-HOS = Tell it to centcom... if you make it that far!!

# Tools interaction

sec-gas-mask-screwed = Changed to {$level} aggression
sec-gas-mask-emagged = Changed to ERROR aggression
ert-gas-mask-impossible = It seems impervious to external change.

-sec-gas-mask-alert-quality =
    { $quality ->
        [cutting] cutting off your mask
        *[screwing] using a screwdriver on your mask
    }

sec-gas-mask-alert-owner = {CAPITALIZE(THE($user))} is { $quality ->
    [cutting] { -sec-gas-mask-alert-quality(quality: "cutting") }[/color]
    *[screwing] { -sec-gas-mask-alert-quality(quality: "screwing") }
} !
#sec-gas-mask-alert-owner = {CAPITALIZE(THE($user))} is cutting off your mask!
#sec-gas-mask-alert-owner = {CAPITALIZE(THE($user))} is using a screwdriver on your mask!
sec-gas-mask-alert-owner-post-emag = {CAPITALIZE(THE($user))} has emagged your mask!

# Examine description
-sec-gas-mask-aggresion-level =
    { $level ->
        [low] Low
        [medium] Medium
        [high] High
       *[other] Unknown
    }

sec-gas-mask-examined = The aggression setting is set to { $level ->
    [low] [color=green]{ -sec-gas-mask-aggresion-level(level: "low") }[/color]
    [medium] [color=yellow]{ -sec-gas-mask-aggresion-level(level: "medium") }[/color]
    [high] [color=red]{ -sec-gas-mask-aggresion-level(level: "high") }[/color]
   *[other] [color=purple]{ -sec-gas-mask-aggresion-level(level: "other") }[/color]
}.

sec-gas-mask-examined-ert = The aggression setting is set to [color=red]High[/color] and doesn't seem capable of being switched.
sec-gas-mask-examined-emagged = The aggression setting is set to [color=red]ERROR[/color]. Weird.
sec-gas-mask-examined-wires-cut = The hailer seems to have its wires cut off.
