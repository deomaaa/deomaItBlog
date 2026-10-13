#!/usr/bin/env bash
# Redeploy DeomaBlog on the VPS: publish, restart, verify.
# Usage: sudo bash deploy/deploy.sh
set -euo pipefail

REPO_DIR="${REPO_DIR:-/root/petprojects/deoma-blog}"
PUBLISH_DIR="${PUBLISH_DIR:-/var/www/deomablog}"
SERVICE="${SERVICE:-deomablog}"
HEALTH_URL="${HEALTH_URL:-http://127.0.0.1:5080/health}"

echo "==> Building from ${REPO_DIR}"
cd "${REPO_DIR}"
git pull --ff-only
dotnet publish src/DeomaBlog.Web/DeomaBlog.Web.csproj -c Release -o "${PUBLISH_DIR}"

echo "==> Restarting ${SERVICE}"
chown -R www-data:www-data "${PUBLISH_DIR}"
systemctl restart "${SERVICE}"

echo "==> Waiting for ${HEALTH_URL}"
for attempt in $(seq 1 30); do
    if curl -fsS "${HEALTH_URL}" >/dev/null; then
        echo "OK after ${attempt}s"
        systemctl --no-pager --lines=5 status "${SERVICE}" | head -12
        exit 0
    fi
    sleep 1
done

echo "Health check failed — last 40 log lines:" >&2
journalctl -u "${SERVICE}" -n 40 --no-pager >&2
exit 1
