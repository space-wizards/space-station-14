## Traitor

# Shown at the end of a round
traitor-round-end-result = {$agentCount ->
    [one] There was one sleeper agent.
    *[other] There were {$agentCount} sleeper agents, with {$activatedagentcount} of them activated.
}
# Shown at the end of a round if sleeper agents were present
agent-user-was-a-sleeper-agent = [color=gray]{$user}[/color] was a unactivated sleeper agent.
agent-user-was-a-sleeper-agent-named = [color=White]{$name}[/color] ([color=gray]{$user}[/color]) was a unactivated sleeper agent.
agent-was-a-sleeper-agent-named = [color=White]{$name}[/color] was a unactivated sleeper agent.
agent-user-was-a-sleeper-agent-with-objectives = [color=gray]{$user}[/color] was a sleeper agent who had the following objectives:
agent-user-was-a-sleeper-agent-with-objectives-named = [color=White]{$name}[/color] ([color=gray]{$user}[/color]) was a sleeper agent who had the following objectives:
agent-was-a-sleeper-agent-with-objectives-named = [color=White]{$name}[/color] was a sleeper agent who had the following objectives:
preset-traitor-objective-issuer-syndicate = [color=#87cefa]The Syndicate[/color]

# ActivatedAgentRole
activatedagent-role-greeting = You have been brainwashed by the Syndicate to be a sleeper agent for their cause. Your objectives and codewords are listed in the character menu. To make sure your cover wasn't blown, the Syndicate did not supply you with any TC. You'll have to obtain your tools through other means.
activatedagent-role-codewords = In the event of openly Syndicate-affiliated agents being present on the station, you can use codewords to discretely identify yourself as a fellow Syndicate member. The syndicate codewords are: {$codewords}
activatedagent-role-phrases = There's a very high chance that other sleeper agents have infiltrated the station you're on. The phrase you'll need to say to them to activate them is: {$phrases}

agent-activation-success = {$name}'s eyes glaze over!
