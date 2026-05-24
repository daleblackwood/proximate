CD=$(dirname $0)
ARTEFACT_PATH="$CD/../Proximate.Deploy/artefacts"
source "$ARTEFACT_PATH/$1.deployment.sh"
ssh -i "$ARTEFACT_PATH/cert" "ec2-user@$DEPLOYMENT_INSTANCE_IP"