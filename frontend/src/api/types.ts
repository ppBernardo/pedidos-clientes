// Contrato da API, espelhando a seção 5 de docs/spec.md.
// Mudou o contrato? Atualize a spec e este arquivo juntos.

export interface Cliente {
  id: number
  nome: string
  email: string
}

export interface Produto {
  id: number
  nome: string
  preco: number
  estoque: number
}

export interface PedidoResumo {
  id: number
  clienteId: number
  clienteNome: string
  /** ISO 8601 em UTC, ex.: 2026-11-12T19:04:11Z */
  dataPedido: string
  quantidadeItens: number
  valorTotal: number
}

export interface ItemPedido {
  produtoId: number
  produtoNome: string
  quantidade: number
  precoUnitario: number
  subtotal: number
}

export interface PedidoDetalhe {
  id: number
  clienteId: number
  clienteNome: string
  dataPedido: string
  valorTotal: number
  itens: ItemPedido[]
}

export interface Pagina<T> {
  itens: T[]
  pagina: number
  tamanhoPagina: number
  totalItens: number
  totalPaginas: number
}

export interface CriarPedidoRequest {
  clienteId: number
  itens: { produtoId: number; quantidade: number }[]
}

/** Filtros da listagem. Datas no formato yyyy-MM-dd (RN-11). */
export interface FiltrosPedidos {
  clienteId?: number
  dataInicio?: string
  dataFim?: string
}

/** RFC 9457. `codigo` vem nos erros de regra de negócio (422). */
export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  traceId?: string
  codigo?: string
  errors?: Record<string, string[]>
}
