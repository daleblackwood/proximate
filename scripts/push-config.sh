#!/bin/bash
echo "[PUSH CONFIG]"
set -e

STACK="$1"
[[ -z $STACK ]] || source "Proximate.Deploy/artefacts/$STACK.deployment.sh"

INSTANCE_IP="${INSTANCE_IP:-$DEPLOYMENT_INSTANCE_IP}"; 

CERT_PATH="Proximate.Deploy/artefacts/cert"
[[ -f "$CERT_PATH" ]] || { echo "Error: certificate is not present."; exit 1; }

REMOTE_USER="ec2-user"
REMOTE_DIR="/home/ec2-user/proximate"
SOURCE_DIR="Proximate/config"

ssh-keyscan -H $INSTANCE_IP >> ~/.ssh/known_hosts

echo "Connecting to $INSTANCE_IP..."
ssh -T -i "$CERT_PATH" "$REMOTE_USER@$INSTANCE_IP" << EOF
    echo "Attempting permission assignment..."
    mkdir -p $REMOTE_DIR
    sudo chown "$REMOTE_USER" "$REMOTE_DIR"
    sudo chmod 755 "$REMOTE_DIR"
EOF

echo "Beginning copy..."
scp -i "$CERT_PATH" -r "$SOURCE_DIR" "$REMOTE_USER@$INSTANCE_IP:$REMOTE_DIR"
echo "Config copy complete."
