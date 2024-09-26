#!/bin/bash
echo "[IMAGE UPDATE]"
set -e

[[ -z $1 ]] || source "Proximate.Deploy/artefacts/$1.deployment.sh"

INSTANCE_IP="${INSTANCE_IP:-$DEPLOYMENT_INSTANCE_IP}"; 
[[ -z "$INSTANCE_IP" ]] && { echo "Error: INSTANCE_IP is not set or deployment not found."; exit 1; }

CERT_PATH="Proximate.Deploy/artefacts/cert"
[[ -f "$CERT_PATH" ]] || { echo "Error: certificate is not present."; exit 1; }

IMAGE_NAME="proximate"

aws ecr get-login-password --region $AWS_REGION | docker login --username AWS --password-stdin $DEPLOYMENT_IMAGE_REPO

echo "Tagging..."
docker tag $IMAGE_NAME:latest $DEPLOYMENT_IMAGE_REPO:latest

echo "Pushing..."
docker push $DEPLOYMENT_IMAGE_REPO:latest
echo "Pushed to $DEPLOYMENT_IMAGE_REPO"

echo "Connecting to $INSTANCE_IP..."
ssh -T -i "$CERT_PATH" "$REMOTE_USER@$INSTANCE_IP" << EOF
    echo "Attempting permission assignment..."
    sudo chown "$REMOTE_USER" "$REMOTE_DIR"
    sudo chmod 755 "$REMOTE_DIR"
EOF

Breaking Down the Script: