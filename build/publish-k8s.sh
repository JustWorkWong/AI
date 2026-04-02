#!/usr/bin/env bash
set -euo pipefail

OUTPUT_PATH="${1:-./artifacts/k8s}"
ENVIRONMENT="${2:-Production}"
APPHOST_PATH="${3:-./src/Wms.AppHost/Wms.AppHost.csproj}"

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
RESOLVED_OUTPUT="${REPO_ROOT}/${OUTPUT_PATH#./}"
RESOLVED_APPHOST="${REPO_ROOT}/${APPHOST_PATH#./}"

if [[ ! -f "${RESOLVED_APPHOST}" ]]; then
  echo "AppHost project not found: ${RESOLVED_APPHOST}" >&2
  exit 1
fi

if ! command -v aspire >/dev/null 2>&1; then
  echo "Aspire CLI not found. Install it with: curl -sSL https://aspire.dev/install.sh | bash" >&2
  exit 1
fi

mkdir -p "${RESOLVED_OUTPUT}"

cd "${REPO_ROOT}"
aspire publish --non-interactive --apphost "${RESOLVED_APPHOST}" --output-path "${RESOLVED_OUTPUT}" --environment "${ENVIRONMENT}"
