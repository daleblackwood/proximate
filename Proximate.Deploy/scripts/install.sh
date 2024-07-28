#!/bin/bash
export MAIN_DIR="/app"
export PROJ_DIR="$MAIN_DIR/project"
export GHR="daleblackwood/proximate"
export GHT="${GITHUB_TOKEN}"
export BRANCH="main"
export CERT="$MAIN_DIR/artefacts/cert"

sudo mkdir -p $MAIN_DIR/artefacts
echo "$DEPLOY_KEY_B64" | base64 -d > $CERT & chmod 400 $CERT

sudo rpm --import https://packages.microsoft.com/keys/microsoft.asc
sudo url -o /etc/yum.repos.d/microsoft-prod.repo https://packages.microsoft.com/config/fedora/39/prod.repo
sudo dnf update -y
sudo dnf install -y dotnet-sdk-8.0

sudo mkdir -p $PROJ_DIR
sudo curl -L -H "Authorization: token $GHT" https://api.github.com/repos/$GHR/tarball/main -o $MAIN_DIR/code.tar.gz

sudo curl -L \
  -H "Accept: application/vnd.github+json" \
  -H "Authorization: Basic $GHT" \
  -H "X-GitHub-Api-Version: 2022-11-28" \
  https://api.github.com/repos/$GHR/tarball
cd $PROJ_DIR
$DOTNET_ROOT/dotnet build
$DOTNET_ROOT/dotnet run