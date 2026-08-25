FROM node:22-bookworm-slim

ARG CODEBASE_MEMORY_VERSION=v0.10.8
ARG SUPERGATEWAY_VERSION=3.2.0
ARG TARGETARCH

RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates curl tar \
    && rm -rf /var/lib/apt/lists/* \
    && npm install -g "supergateway@${SUPERGATEWAY_VERSION}"

RUN set -eux; \
    case "${TARGETARCH}" in \
      amd64) cbm_arch=amd64 ;; \
      arm64) cbm_arch=arm64 ;; \
      *) echo "Unsupported architecture: ${TARGETARCH}" >&2; exit 1 ;; \
    esac; \
    asset="codebase-memory-mcp-linux-${cbm_arch}-portable.tar.gz"; \
    base="https://github.com/DeusData/codebase-memory-mcp/releases/download/${CODEBASE_MEMORY_VERSION}"; \
    curl -fsSLo /tmp/checksums.txt "${base}/checksums.txt"; \
    curl -fsSLo "/tmp/${asset}" "${base}/${asset}"; \
    expected="$(awk -v asset="${asset}" '$2 == asset || $2 == "*" asset { print $1; exit }' /tmp/checksums.txt)"; \
    test -n "${expected}"; \
    actual="$(sha256sum "/tmp/${asset}" | awk '{print $1}')"; \
    test "${actual}" = "${expected}"; \
    mkdir -p /tmp/cbm; \
    tar -xzf "/tmp/${asset}" -C /tmp/cbm; \
    binary="$(find /tmp/cbm -type f -name codebase-memory-mcp -print -quit)"; \
    test -n "${binary}"; \
    install -m 0755 "${binary}" /usr/local/bin/codebase-memory-mcp; \
    /usr/local/bin/codebase-memory-mcp --version; \
    rm -rf /tmp/cbm "/tmp/${asset}" /tmp/checksums.txt

ENV CBM_CACHE_DIR=/var/lib/codebase-memory
WORKDIR /workspace

EXPOSE 9750

HEALTHCHECK --interval=15s --timeout=5s --retries=5 --start-period=10s \
  CMD curl -fsS http://127.0.0.1:9750/healthz || exit 1

ENTRYPOINT ["supergateway"]
CMD ["--stdio", "codebase-memory-mcp", "--outputTransport", "streamableHttp", "--stateful", "--sessionTimeout", "300000", "--port", "9750", "--streamableHttpPath", "/mcp", "--healthEndpoint", "/healthz", "--logLevel", "info"]
