#!/usr/bin/env bash
# Deploys the kiosk. Usage: infra/deploy.sh [all|api|web]   (default: all)
#
#   api  publishes SandKey.Api.Display for linux-arm64, zips it, and runs sam deploy
#   web  builds ../sandkey-display-web, syncs dist/ to the web bucket, invalidates CloudFront
#
# Secrets are never touched here; the API reads them from Parameter Store at startup.
set -euo pipefail
cd "$(dirname "$0")"

TARGET="${1:-all}"
STACK=sandkey-display
REGION=us-east-1
WEB_DIR=../../sandkey-display-web

stack_output() {
  aws cloudformation describe-stacks --stack-name "$STACK" --region "$REGION" \
    --query "Stacks[0].Outputs[?OutputKey=='$1'].OutputValue" --output text
}

if [[ "$TARGET" == "all" || "$TARGET" == "api" ]]; then
  echo "== Publishing API"
  rm -rf .publish api.zip
  dotnet publish ../SandKey.Api.Display/SandKey.Api.Display.csproj \
    --configuration Release --runtime linux-arm64 --self-contained false \
    -p:PublishReadyToRun=true --output .publish
  (cd .publish && zip -qr ../api.zip .)
  echo "== Deploying stack $STACK"
  sam deploy
fi

if [[ "$TARGET" == "all" || "$TARGET" == "web" ]]; then
  echo "== Building web"
  (cd "$WEB_DIR" && npm ci --no-audit --no-fund && npm run build)
  BUCKET=$(stack_output WebBucketName)
  DIST=$(stack_output DistributionId)
  echo "== Syncing to s3://$BUCKET"
  aws s3 sync "$WEB_DIR/dist" "s3://$BUCKET" --delete --region "$REGION"
  echo "== Invalidating $DIST"
  aws cloudfront create-invalidation --distribution-id "$DIST" --paths '/*' --query 'Invalidation.Id' --output text
fi

echo "== Done: $(stack_output KioskUrl)"
