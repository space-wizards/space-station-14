# Sickness

| Designers | Coders | Implemented | GitHub Links |
|---|---|---|---|
| JesterX | JesterX | No | TBD |

## Overview

A basic sickness system that inflict a variety of ailments to the crew of the station.  They can be viral, bacterial and psychologics in nature.
All sickness are made of:
- The name of the sickness
- A kind of sickness cause (viral, bacterial or psychologic).  That will impact how the sickness can be cured later on.
- A collection of sickness stages.

Sickness stages are different stages of each sickness, when they appear, what they do.

Description of what a sickness stage is:
- Time until that stage begins
- Duration of said stage
- What it does

Kind of stages that will be included in first release:

- Cough  
  the player will cough, spreading germs near him, possibly infecting other crew member (see contagion below)
  This will appear a an emote
  Is the cough contagious or not
- Sneeze
  the player will sneeze, spreading germs near him, possibly infecting other crew member (see contagion below)
  This will appear as an emote
  Is the sneeze contagious or not



## Background

Summarize any information that is needed to contextualize the proposed changes, e.g. the current state of the game.

Also link any relevant discussions on Discord, GitHub, or HackMD that are relevant to the proposal.

## Features to be added

Give a description of what game mechanics you would like to add or change. This should be a general overview, with enough details on critical design points that someone can directly implement the feature from this design document. Exact numbers for game balance however are not necessary, as these can be adjusted later either during development or after it has been implemented, but mention *what* will have to be balanced and what needs to be considered when doing so.

## Game Design Rationale

Consider addressing:
- How does the feature align with our [Core Design Principles](../space-station-14/core-design/design-principles.md) and game philosphy?
- What makes this feature enjoyable or rewarding for players?
- Does it introduce meaningful choices, risk vs. reward, or new strategies?
- How does it enhance player cooperation, competition, or emergent gameplay?
- If the feature is a new antagonist, how does it fit into the corresponding [design pillars](../space-station-14/round-flow/antagonists.md)?

## Roundflow & Player interaction

Consider addressing:
- At what point in the round does the feature come into play? Does it happen every round? How does it affect the round pace?
- How do you wish for players to interact with your feature and how should they not interact with it? How is this mechanically enforced?
- Which department will interact with the feature? How does the feature fit into the [design document](../space-station-14/departments.md) for that department?

## Administrative & Server Rule Impact (if applicable)

- Does this feature introduce any new rule enforcement challenges or additional workload for admins?
- Could this feature increase the likelihood of griefing, rule-breaking, or player disputes?
- How are the rules enforced mechanically by way the feature will be implemented?

# Technical Considerations

- Does the feature require new systems, UI elements, or refactors of existing ones? Give a short technical outline on how they will be implemented.
- Are there any anticipated performance impacts?
- For required UI elements, give a short description or a mockup of how they should look like (for example a radial menu, actions & alerts, navmaps, or other window types)
