import type { FiltrosPedidos } from './types.ts'

/** Chaves do TanStack Query num só lugar, para invalidar sem erro de digitação. */
export const queryKeys = {
  pedidos: ['pedidos'] as const,
  listaPedidos: (filtros: FiltrosPedidos, pagina: number, tamanhoPagina: number) =>
    ['pedidos', 'lista', { ...filtros, pagina, tamanhoPagina }] as const,
  clientes: ['clientes'] as const,
  produtos: ['produtos'] as const,
}
