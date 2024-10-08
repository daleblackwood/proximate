#!/bin/bash
echo "[NET PUSH]"
set -e

STACK="$1"
[[ -z $STACK ]] || source "Proximate.Deploy/artefacts/$STACK.deployment.sh"

INSTANCE_IP="${INSTANCE_IP:-$DEPLOYMENT_INSTANCE_IP}"
[[ -z "$INSTANCE_IP" ]] && { echo "Error: stack not set (arg) or deployment not found."; exit 1; }

CERT_PATH="Proximate.Deploy/artefacts/cert"
[[ -f "$CERT_PATH" ]] || { echo "Error: certificate is not present."; exit 1; }

REMOTE_USER="ec2-user"
REMOTE_DIR="/home/$REMOTE_USER/proximate"
SOURCE_DIR="Proximate/config"

echo "Building..."
dotnet publish --configuration Release --runtime linux-arm64

echo "Connecting to $INSTANCE_IP..."
ssh -T -i "$CERT_PATH" "$REMOTE_USER@$INSTANCE_IP" << EOF
    sudo systemctl disable --now proximate.service

    echo 
    echo "Installing remote dotnet..."
    set -e
    sudo dnf install -y libicu
    sudo curl -sSL https://dot.net/v1/dotnet-install.sh -o dotnet-install.sh
    sudo chmod +x dotnet-install.sh
    sudo ./dotnet-install.sh --channel 9.0 --runtime dotnet --install-dir /opt/dotnet

    rm -rf "$REMOTE_DIR/bin"
    sudo mkdir -p "$REMOTE_DIR/bin"
    sudo mkdir -p "$REMOTE_DIR/logs"
    sudo mkdir -p "$REMOTE_DIR/config"
    sudo chown -R $REMOTE_USER .

    echo 
    echo "Setting up remote service..."
    sudo tee /etc/systemd/system/proximate.service > /dev/null << SERVICE
[Unit]
Description=Proximate .NET Application
After=network.target

[Service]
ExecStart=/opt/dotnet/dotnet /home/ec2-user/proximate/bin/Proximate.dll
WorkingDirectory=/home/ec2-user/proximate
Restart=always

[Install]
WantedBy=multi-user.target
SERVICE

    sudo systemctl daemon-reexec
    sudo systemctl enable proximate
    sudo systemctl restart proximate
    echo 
    echo "Remote setup complete."
EOF

echo 
echo "Beginning copy..."
scp -r -i "$CERT_PATH" "./Proximate/bin/Release/net9.0/linux-arm64/publish/"* "$REMOTE_USER@$INSTANCE_IP:$REMOTE_DIR/bin/"
scp -r -i "$CERT_PATH" "./Proximate/config/" "$REMOTE_USER@$INSTANCE_IP:$REMOTE_DIR/"
scp -r -i "$CERT_PATH" "./Proximate/public/" "$REMOTE_USER@$INSTANCE_IP:$REMOTE_DIR/"

echo "Config copy complete."

echo 
echo "Starting service on remote instance..."
ssh -T -i "$CERT_PATH" "$REMOTE_USER@$INSTANCE_IP" << EOF
    set -e
    sudo systemctl enable --now proximate.service
    echo Proximate running.
EOF
echo 
echo "Proximate deployed to $INSTANCE_IP"

scp -r -i "$CERT_PATH" ./logs/dev/logs/* "$REMOTE_USER@$INSTANCE_IP:$REMOTE_DIR/logs"