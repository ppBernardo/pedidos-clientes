#!/usr/bin/env bash
# Inicializa o banco: cria o banco, as tabelas, os índices e os logins, e aplica
# a carga de exemplo.
#
# Idempotente. É o mesmo caminho em todos os ambientes:
#   * docker compose: serviço db-init (DB_HOST=sqlserver), roda a cada `up`;
#   * testes de integração: a fixture do Testcontainers executa este script
#     dentro do container do SQL Server (DB_HOST=localhost).
#
# Rode sempre com `bash init-db.sh`: o bit de execução não é versionado a
# partir do Windows.
set -euo pipefail

DB_HOST="${DB_HOST:-sqlserver}"
DB_NAME="${DB_NAME:-PedidosDb}"
APP_DB_PASSWORD="${APP_DB_PASSWORD:-Pedidos2026App}"
MCP_DB_PASSWORD="${MCP_DB_PASSWORD:-Pedidos2026Mcp}"
CARGA_EXEMPLO="${CARGA_EXEMPLO:-1}"
: "${SA_PASSWORD:?Defina SA_PASSWORD}"

DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SQLCMD="$(find /opt/mssql-tools*/bin/sqlcmd -type f -print -quit 2>/dev/null || true)"
if [[ -z "$SQLCMD" ]]; then
  echo "sqlcmd não encontrado em /opt/mssql-tools*/bin" >&2
  exit 1
fi

# Senhas só por variável de ambiente, nunca na linha de comando (que aparece na
# lista de processos): o sqlcmd lê SQLCMDPASSWORD para o login e resolve as
# variáveis $(DB_NAME), $(APP_DB_PASSWORD) e $(MCP_DB_PASSWORD) dos scripts a
# partir do ambiente.
export SQLCMDPASSWORD="$SA_PASSWORD"
export DB_NAME APP_DB_PASSWORD MCP_DB_PASSWORD

# -C: confia no certificado autoassinado (o ODBC 18 criptografa por padrão)
# -b: falha no primeiro erro, para a API não subir sobre um banco quebrado
# -f 65001: lê os scripts como UTF-8 (acentos da carga)
sql() { "$SQLCMD" -S "$DB_HOST" -U sa -C -b -f 65001 "$@"; }

echo "Aguardando o SQL Server em $DB_HOST..."
for tentativa in $(seq 1 60); do
  if sql -d master -Q "SET NOCOUNT ON; SELECT 1" -o /dev/null 2>/dev/null; then
    break
  fi
  if [[ "$tentativa" -eq 60 ]]; then
    echo "SQL Server não respondeu a tempo." >&2
    exit 1
  fi
  sleep 2
done

executar() {
  local banco="$1" arquivo="$2"
  shift 2
  echo ">> $arquivo"
  sql -d "$banco" -i "$DIR/$arquivo" "$@"
}

executar master     01-criar-banco.sql
executar "$DB_NAME" 02-tabelas.sql
executar "$DB_NAME" 03-indices.sql
executar "$DB_NAME" 04-seguranca.sql

if [[ "$CARGA_EXEMPLO" == "1" ]]; then
  executar "$DB_NAME" 05-carga-dados.sql
fi

echo "Banco $DB_NAME pronto."
