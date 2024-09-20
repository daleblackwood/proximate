#!/bin/bash
set -e
source Proximate.Deploy/artefacts/output.sh

INSTANCE_IP="${INSTANCE_IP:-$LATEST_INSTANCE_IP}"; 
[[ -z "$INSTANCE_IP" ]] && { echo "Error: INSTANCE_IP is not set."; exit 1; }

CERT_PATH="Proximate.Deploy/artefacts/cert"
REMOTE_USER="ec2-user"
REMOTE_DIR="/home/ec2-user/proximate/config"
SOURCE_DIR="Proximate/config"

ssh -i "$CERT_PATH" "$INSTANCE_IP:$REMOTE_DIR" << EOF
    sudo chmod 
EOF

echo scp -i "$CERT_PATH" -r "$SOURCE_DIR" "$REMOTE_USER@$INSTANCE_IP:$REMOTE_DIR"

scp -i "$CERT_PATH" -r "$SOURCE_DIR" "$REMOTE_USER@$INSTANCE_IP:$REMOTE_DIR"
