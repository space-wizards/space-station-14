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
wizard-description = There's a Wizard on the station! You never know what they might do.

roles-antag-wizard-name = Wizard
roles-antag-wizard-objective = Teach them a lesson they'll never forget.

wizard-role-greeting =
    It's wizard time, fireball!
    There's been tensions between the Space Wizards Federation and NanoTrasen. You've been selected by the Space Wizards Federation to pay a visit to the station and "remind them" why spellcasters are not to be trifled with.
    Cause mayhem and destruction! What you do is up to you, but remember that the Space Wizards want you to make it out alive.

wizard-round-end-name = wizard

## TODO: Wizard Apprentice (Coming sometime post-wizard release)
