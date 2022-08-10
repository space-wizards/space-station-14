git submodule add https://github.com/moonheart08/outer-rim-14-secrets Secrets
New-Item -ItemType SymbolicLink -Path "Content.ServerSecret\Code" -Target "Secrets\ServerSecret"
New-Item -ItemType SymbolicLink -Path "Resources\ServerPrototypes" -Target "Secrets\ServerPrototypes"
