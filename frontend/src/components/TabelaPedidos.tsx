import type { PedidoResumo } from '../api/types.ts'
import { formatarDataHora, formatarMoeda } from '../utils/formatacao.ts'

interface TabelaPedidosProps {
  pedidos: PedidoResumo[]
  /** Pedido recém-criado, destacado na tabela. */
  destaqueId?: number
}

export function TabelaPedidos({ pedidos, destaqueId }: TabelaPedidosProps) {
  return (
    <div className="tabela-rolagem">
      <table className="tabela">
        <thead>
          <tr>
            <th scope="col">Pedido</th>
            <th scope="col">Cliente</th>
            <th scope="col">Data</th>
            <th scope="col" className="numero">
              Itens
            </th>
            <th scope="col" className="numero">
              Valor total
            </th>
          </tr>
        </thead>
        <tbody>
          {pedidos.map((pedido) => (
            <tr key={pedido.id} className={pedido.id === destaqueId ? 'tabela__linha--destaque' : undefined}>
              <td>#{pedido.id}</td>
              <td>{pedido.clienteNome}</td>
              <td>
                <time dateTime={pedido.dataPedido}>{formatarDataHora(pedido.dataPedido)}</time>
              </td>
              <td className="numero">{pedido.quantidadeItens}</td>
              <td className="numero">{formatarMoeda(pedido.valorTotal)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
