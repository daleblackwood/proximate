#!/bin/bash
mkdir -p ./config
chmod u+rx ./config
chmod -R u+rx ./config
cp -r ./config_default/* ./config
dotnet Proximate.dll