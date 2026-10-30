## Survivor

roles-antag-survivor-name = Survivor
# It's a Halo reference
roles-antag-survivor-objective = Current Objective: Survive

survivor-role-greeting =
    You are a Survivor. Above all you need to make it back to Central Command alive.
    Collect as much firepower as needed to guarantee your survival.
    Trust no one.

survivor-round-end-dead-count =
{
    $deadCount ->
        [one] [color=red]{$deadCount}[/color] survivor died.
        *[other] [color=red]{$deadCount}[/color] survivors died.
}

survivor-round-end-alive-count =
{
    $aliveCount ->
        [one] [color=yellow]{$aliveCount}[/color] survivor was marooned on the station.
        *[other] [color=yellow]{$aliveCount}[/color] survivors were marooned on the station.
}

survivor-round-end-alive-on-shuttle-count =
{
    $aliveCount ->
        [one] [color=green]{$aliveCount}[/color] survivor made it out alive.
        *[other] [color=green]{$aliveCount}[/color] survivors made it out alive.
}

## Wizard

objective-issuer-swf = [color=turquoise]The Space Wizards Federation[/color]

wizard-title = Wizard
wizard-description = There's a wizard on the station! You never know what they might do.

roles-antag-wizard-name = Wizard
roles-antag-wizard-objective = Use your mastery of the arcane to sow chaos and discord.

wizard-role-greeting =
    It's wizard time!
    Tensions between the Space Wizards Federation and Nanotrasen are on the rise. You've been selected to pay a visit to one of Nanotrasen's space stations and "remind" them why wizards are not to be trifled with.
    Cause mayhem and destruction! What you do is up to you, but remember that your superiors want you to make it out alive.

wizard-round-end-name = wizard

## TODO: Wizard Apprentice (Coming sometime post-wizard release)
