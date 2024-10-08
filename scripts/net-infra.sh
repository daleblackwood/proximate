#!/bin/bash
echo "[NET INFRA]"
set -e

export $(cat Proximate.Deploy/artefacts/dev.env | xargs)
aws sts get-caller-identity --query "Arn" --output text

pulumi up --cwd ./Proximate.Deploy -s dev2