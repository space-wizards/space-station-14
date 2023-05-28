#!/usr/bin/env sh

# make sure to start from script dir
if [ "$(dirname $0)" != "." ]; then
    cd "$(dirname $0)"
fi

cd ../../
dotnet run --project Content.Client --no-build
