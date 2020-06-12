FROM mcr.microsoft.com/dotnet/core/sdk:3.1-alpine AS sdk

RUN apk add python3

COPY . /App

RUN cd /App && dotnet build -c Debug

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-alpine

COPY --from=sdk /App /App

ENTRYPOINT ["dotnet", "/App/bin/Content.Server/Content.Server.dll"]