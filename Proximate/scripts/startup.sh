#!/bin/bash
mkdir -p ./config
sudo chmod u+rx ./config/
sudo chmod -R u+rx ./config/
cp -r ./config_default/* ./config/
dotnet Proximate.dll