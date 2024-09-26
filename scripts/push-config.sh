#!/bin/bash
echo "[PUSH CONFIG]"
set -e

[[ -z $1 ]] || source "Proximate.Deploy/artefacts/$1.deployment.sh"

INSTANCE_IP="${INSTANCE_IP:-$DEPLOYMENT_INSTANCE_IP}"; 
[[ -z "$INSTANCE_IP" ]] && { echo "Error: INSTANCE_IP is not set or deployment not found."; exit 1; }

CERT_PATH="Proximate.Deploy/artefacts/cert"
[[ -f "$CERT_PATH" ]] || { echo "Error: certificate is not present."; exit 1; }

REMOTE_USER="ec2-user"
REMOTE_DIR="/home/ec2-user/proximate/config"
SOURCE_DIR="Proximate/config"

echo "Connecting to $INSTANCE_IP..."
ssh -T -i "$CERT_PATH" "$REMOTE_USER@$INSTANCE_IP" << EOF
    echo "Attempting permission assignment..."
    sudo chown "$REMOTE_USER" "$REMOTE_DIR"
    sudo chmod 755 "$REMOTE_DIR"
EOF

echo "Beginning copy..."
scp -i "$CERT_PATH" -r "$SOURCE_DIR" "$REMOTE_USER@$INSTANCE_IP:$REMOTE_DIR"
echo "Config copy complete."
