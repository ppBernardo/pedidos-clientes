#!/usr/bin/env bash
# Parte 4: mede a query de ranking (original x corrigida, sem e com índices)
# num banco com volume, o PedidosPerf, separado do banco da aplicação.
#
# Rodar com o banco do compose no ar (reaproveita o serviço db-init, que já tem
# o sqlcmd e a pasta database/ montada):
#   MSYS_NO_PATHCONV=1 docker compose run --rm -v ./docs/parte4:/saida --entrypoint bash db-init /database/perf/run-perf.sh
#
# Saída em docs/parte4/: estatísticas (.txt), planos reais (.sqlplan, que abrem
# no SSMS ou na extensão mssql do VS Code) e resumo.txt.
set -euo pipefail

DB_HOST="${DB_HOST:-sqlserver}"
DB=PedidosPerf
SAIDA="${SAIDA:-/saida}"
: "${SA_PASSWORD:?Defina SA_PASSWORD}"

PERF="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
RAIZ="$(dirname "$PERF")"
ORIGINAL="$RAIZ/consultas/ranking-2026-original.sql"
CORRIGIDA="$RAIZ/consultas/ranking-2026-corrigida.sql"
SQLCMD="$(find /opt/mssql-tools*/bin/sqlcmd -type f -print -quit)"
export SQLCMDPASSWORD="$SA_PASSWORD"
sql() { "$SQLCMD" -S "$DB_HOST" -U sa -C -b -f 65001 "$@"; }

mkdir -p "$SAIDA"

echo ">> Banco $DB com as tabelas, ainda sem os índices de suporte"
sql -d master -i "$RAIZ/01-criar-banco.sql" -v DB_NAME="$DB"
sql -d "$DB" -i "$RAIZ/02-tabelas.sql"
sql -d "$DB" -i "$PERF/remover-indices.sql"

echo ">> Gerando volume (pode levar alguns minutos na primeira vez)"
sql -d "$DB" -i "$PERF/01-gerar-volume.sql"

# Mede uma consulta: aquece o cache, coleta STATISTICS IO/TIME e salva o plano real.
medir() {
  local rotulo="$1" consulta="$2"
  echo ">> Medindo: $rotulo"
  sql -d "$DB" -i "$consulta" -o /dev/null
  sql -d "$DB" -v ARQUIVO="$consulta" -i "$PERF/medir.sql" -r1 1>/dev/null 2>"$SAIDA/$rotulo.txt"
  sql -d "$DB" -v ARQUIVO="$consulta" -i "$PERF/plano.sql" -y 0 -h -1 \
    | grep -o '<ShowPlanXML.*</ShowPlanXML>' > "$SAIDA/$rotulo.sqlplan"
}

medir 1-original-sem-indices "$ORIGINAL"
medir 2-corrigida-sem-indices "$CORRIGIDA"

echo ">> Criando os índices de suporte (database/03-indices.sql)"
sql -d "$DB" -i "$RAIZ/03-indices.sql"
sql -d "$DB" -Q "UPDATE STATISTICS dbo.Pedidos WITH FULLSCAN; UPDATE STATISTICS dbo.ItensPedido WITH FULLSCAN;"

medir 3-original-com-indices "$ORIGINAL"
medir 4-corrigida-com-indices "$CORRIGIDA"

echo ">> Resumo (leituras lógicas por tabela e tempos)"
for arquivo in "$SAIDA"/[1-4]-*.txt; do
  echo "== $(basename "$arquivo" .txt)"
  grep -E "Table '|CPU time" "$arquivo" | sed 's/^ *//'
done | tee "$SAIDA/resumo.txt"
