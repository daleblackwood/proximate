#!/bin/bash
source Proximate.Deploy/artefacts/output.sh

INSTANCE_IP="$OUTPUT_INSTANCE_IP"
CERT_PATH="Proximate.Deploy/artefacts/cert"
REMOTE_USER="ec2-user"
REMOTE_DIR="/home/ec2-user/proximate/logs"
SANITIZED_HOSTNAME=$(echo "$INSTANCE_IP" | tr '.' '.')
TIMESTAMPED=0
LOCAL_DEST="./logs/"
TS_FORMAT="%Y-%m-%d"

while [[ $# -gt 0 ]]; do
    case "$1" in
        --new) TIMESTAMPED=1 ;;
        --dest) LOCAL_DEST="$2"; shift ;;
        --ip) INSTANCE_IP="$2"; shift ;;
    esac
    shift
done

if [ $TIMESTAMPED -ne 0 ]; then 
    TS_FORMAT="%Y-%m-%dT%H:%M:%S"
fi

TARBALL_NAME="ps_$(date +$TS_FORMAT)_${SANITIZED_HOSTNAME}.tar.gz"

echo "Compressing logs on remote server..."
ssh -i "$CERT_PATH" "$REMOTE_USER@$INSTANCE_IP" "tar -czf /tmp/$TARBALL_NAME -C $(dirname "$REMOTE_DIR") $(basename "$REMOTE_DIR")"

echo "Downloading logs..."
scp -i "$CERT_PATH" "$REMOTE_USER@$INSTANCE_IP:/tmp/$TARBALL_NAME" "$LOCAL_DEST"

echo "Cleaning up remote tarball..."
ssh -i "$CERT_PATH" "$REMOTE_USER@$INSTANCE_IP" "rm /tmp/$TARBALL_NAME"

echo "Download complete. Logs saved to $LOCAL_DEST$TARBALL_NAME"
