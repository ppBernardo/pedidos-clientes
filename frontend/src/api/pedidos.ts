import { requisicao } from './client.ts'
import type {
  Cliente,
  CriarPedidoRequest,
  FiltrosPedidos,
  Pagina,
  PedidoDetalhe,
  PedidoResumo,
  Produto,
} from './types.ts'

export function listarPedidos(filtros: FiltrosPedidos, pagina: number, tamanhoPagina: number) {
  const parametros = new URLSearchParams({
    pagina: String(pagina),
    tamanhoPagina: String(tamanhoPagina),
  })
  if (filtros.clienteId) parametros.set('clienteId', String(filtros.clienteId))
  if (filtros.dataInicio) parametros.set('dataInicio', filtros.dataInicio)
  if (filtros.dataFim) parametros.set('dataFim', filtros.dataFim)
  return requisicao<Pagina<PedidoResumo>>(`/pedidos?${parametros}`)
}

export function criarPedido(pedido: CriarPedidoRequest) {
  return requisicao<PedidoDetalhe>('/pedidos', { method: 'POST', body: JSON.stringify(pedido) })
}

export function listarClientes() {
  return requisicao<Cliente[]>('/clientes')
}

export function listarProdutos() {
  return requisicao<Produto[]>('/produtos')
}
