git submodule add -f https://github.com/ekrixi-14/secrets Secrets
New-Item -ItemType SymbolicLink -Path "Content.ServerSecret\Code" -Target "Secrets\ServerCode"
New-Item -ItemType SymbolicLink -Path "Resources\Prototypes\SecretPrototypes" -Target "Secrets\SecretResources\Prototypes"
pause
