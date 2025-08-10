# SPDX-FileCopyrightText: 2024 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
# SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
#
# SPDX-License-Identifier: AGPL-3.0-or-later

cd ../../

mkdir Scripts/logs

rm Scripts/logs/Content.IntegrationTests.log
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj -c DebugOpt -- NUnit.ConsoleOut=0 NUnit.MapWarningTo=Failed > Scripts/logs/Content.IntegrationTests.log

echo "Tests complete. Press enter to continue."
read
