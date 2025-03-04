vampires-title = Vampires

vampire-fangs-extended-examine = You see a glint of [color=white]sharp teeth[/color]
vampire-fangs-extended = You extend your fangs
vampire-fangs-retracted = You retract your fangs

vampire-blooddrink-empty = This body is devoid of blood
vampire-blooddrink-rotted = Their body is rotting and their blood tainted
vampire-blooddrink-zombie = Their blood is tainted by death

vampire-startlight-burning = You feel your skin burn in the light of a thousand suns

vampire-not-enough-blood = You dont have enough blood
vampire-cuffed = You need your hands free!
vampire-stunned = You cant concentrate enough!
vampire-muffled = Your mouth is muzzled
vampire-full-stomach = You are bloated with blood

vampire-deathsembrace-bind = Feels like home

vampire-ingest-holyblood = Your mouth burns!

vampire-cloak-enable = You wrap shadows around your form
vampire-cloak-disable = You release your grip on the shadows

vampire-bloodsteal-other = You feel blood being ripped from your body!
vampire-bloodsteal-no-victims = You're trying to steal blood, but there are no victims around, your powers are dissipating into thin air!
vampire-hypnotise-other = {CAPITALIZE(THE($user))} stares deeply into {MAKEPLURAL(THE($target))} eyes!
vampire-unnaturalstrength = The upper muscles {CAPITALIZE(THE($user))} increase making him stronger!
vampire-supernaturalstrength = The upper muscles of {CAPITALIZE(THE($user))} swell with power making him super strong!

store-currency-display-blood-essence = Blood Essence
store-category-vampirepowers = Powers
store-category-vampirepassives = Passives

# Powers

# Passives
vampire-passive-unholystrength = Unholy Strength
vampire-passive-unholystrength-description = Infuse your upper body muscles with essence, granting you claws and increased strength. Effect: 10 Slash per hit

vampire-passive-supernaturalstrength = Supernatural Strength
vampire-passive-supernaturalstrength-description = Increase your upper body muscles strength further, no barrier shall stand in your way. Effect: 15 Slash per hit, able to pry open doors by hand.

vampire-passive-deathsembrace = Deaths Embrace
vampire-passive-deathsembrace-description = Embrace death and it shall pass you over. Effect: Heal when in a coffin, automatically return to your coffin upon death for 100 blood essence.

# Mutation menu

vampire-mutation-menu-ui-window-name = Mutation menu

vampire-mutation-none-info = Nothing selected

vampire-mutation-hemomancer-info = 
    Hemomancer
    
    Focuses on blood magic and manipulating the blood around him.
    
    Abilities:
    
    - Screech
    - Blood Steal

vampire-mutation-umbrae-info = 
    Shadow
    
    Focuses on darkness, stealth, mobility.
    
    Abilities:
    
    - Glare
    - Cloak of Darkness
    
vampire-mutation-gargantua-info = 
    Gargantua
    
    Focuses on melee damage and resilience.
    
    Abilities:
    
    - Unholy Strength
    - Supernatural Strength

vampire-mutation-bestia-info = 
    Bestia
    
    Focuses on turning and collecting trophies.
    
    Abilities:
    
    - Bat Form
    - Mouse Form
    
## Objectives

objective-condition-drain-title = Drain { $count } blood.
objective-condition-drain-description = I must drink { $count } of blood. It is necessary for my survival and further evolution.
ent-VampireSurviveObjective = Survive
    .desc = I have to survive, whatever it takes.
ent-VampireEscapeObjective = Fly off the station alive and free.
    .desc = I'm supposed to leave on an escape shuttle. Free.
    
## Alert

alerts-vampire-blood-name = Blood Essence Amount
alerts-vampire-blood-desc = Amount of vampire blood essence.
alerts-vampire-stellar-weakness-name = Stellar Weakness
alerts-vampire-stellar-weakness-desc = You are burnt by the light of the sun, or to be specific - the few billion stars you are exposed to outside the station.


## Preset

vampire-roundend-name = Vampire
objective-issuer-vampire = [color=red]Bloodlust[/color]
roundend-prepend-vampire-drained-named = [color=white]{ $name }[/color] drank a total of [color=red]{ $number }[/color] blood.
roundend-prepend-vampire-drained = Someone drank a total of [color=red]{ $number }[/color] blood.
vampire-gamemode-title = Vampires
vampire-gamemode-description = Bloodthirsty vampires have infiltrated the station to drink blood!
vampire-role-greeting =
    You are a vampire who sneaked into the station disguised as an employee!
        Your tasks are listed in the character menu.
        Drink blood and evolve to accomplish them!
vampire-role-greeting-short = You are a vampire who sneaked into the station disguised as an employee!
roles-antag-vamire-name = Vampire

## Actions

ent-ActionVampireOpenMutationsMenu = Mutation menu
    .desc = Opens a menu with vampire mutations.
ent-ActionVampireToggleFangs = Toggle Fangs
    .desc = Extend or retract your fangs. Walking around with your fangs out might reveal your true nature.
ent-ActionVampireGlare = Glare
    .desc = Release a blinding flash from your eyes, stunning a unprotected mortal for 10 seconds. Activation Cost: 20 Essence. Cooldown: 60 Seconds
ent-ActionVampireHypnotise = Hypnotise
    .desc = Stare deeply into a mortals eyes, forcing them to sleep for 60 seconds. Activation Cost: 20 Essence. Activation Delay: 5 Seconds. Cooldown: 5 Minutes
ent-ActionVampireScreech = Screech
    .desc = Release a piercing scream, stunning unprotected mortals and shattering fragile objects nearby. Activation Cost: 20 Essence. Activation Delay: 5 Seconds. Cooldown: 5 Minutes
ent-ActionVampireBloodSteal = Blood Steal
    .desc = Wrench the blood from all bodies nearby - living or dead. Activation Cost: 20 Essence. Cooldown: 60 Seconds
ent-ActionVampireBatform = Bat Form
    .desc = Assume for form of a bat. Fast, Hard to Hit, Likes fruit. Activation Cost: 20 Essence. Cooldown: 30 Seconds
ent-ActionVampireMouseform = Mouse Form
    .desc = Assume for form of a mouse. Fast, Small, Immune to doors. Activation Cost: 20 Essence. Cooldown: 30 Seconds
ent-ActionVampireCloakOfDarkness = Cloak of Darkness
    .desc = Cloak yourself from mortal eyes, rendering you invisible while stationary. Blood to Activation: 330 Essence, Activation Cost: 30 Essence. Upkeep: 1 Essence/Second Cooldown: 10 Seconds