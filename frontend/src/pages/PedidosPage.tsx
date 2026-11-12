import { useState } from 'react'
import type { FiltrosPedidos } from '../api/types.ts'
import { Filtros } from '../components/Filtros.tsx'
import { Paginacao } from '../components/Paginacao.tsx'
import { TabelaPedidos } from '../components/TabelaPedidos.tsx'
import { useClientes, usePedidos } from '../hooks/usePedidos.ts'

const TAMANHO_PAGINA = 10

export function PedidosPage() {
  const [filtros, setFiltros] = useState<FiltrosPedidos>({})
  const [pagina, setPagina] = useState(1)

  const clientes = useClientes()
  const pedidos = usePedidos(filtros, pagina, TAMANHO_PAGINA)

  function aplicarFiltros(novos: FiltrosPedidos) {
    setFiltros(novos)
    setPagina(1)
  }

  return (
    <section aria-labelledby="titulo-pedidos">
      <div className="cabecalho-pagina">
        <h1 id="titulo-pedidos">Pedidos</h1>
      </div>

      <Filtros
        clientes={clientes.data ?? []}
        clientesIndisponiveis={clientes.isError}
        onAplicar={aplicarFiltros}
      />

      <div className="cartao" aria-busy={pedidos.isFetching}>
        {pedidos.isPending ? (
          <p className="estado">Carregando pedidos…</p>
        ) : pedidos.isError ? (
          <div className="estado estado--erro" role="alert">
            <p>Não foi possível carregar os pedidos: {pedidos.error.message}</p>
            <button type="button" className="botao" onClick={() => pedidos.refetch()}>
              Tentar novamente
            </button>
          </div>
        ) : pedidos.data.itens.length === 0 ? (
          <p className="estado">Nenhum pedido encontrado para os filtros informados.</p>
        ) : (
          <>
            <TabelaPedidos pedidos={pedidos.data.itens} />
            <Paginacao
              pagina={pedidos.data.pagina}
              totalPaginas={pedidos.data.totalPaginas}
              totalItens={pedidos.data.totalItens}
              onMudarPagina={setPagina}
            />
          </>
        )}
      </div>
    </section>
  )
}
