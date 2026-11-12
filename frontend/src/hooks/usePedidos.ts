import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { criarPedido, listarClientes, listarPedidos, listarProdutos } from '../api/pedidos.ts'
import { queryKeys } from '../api/queryKeys.ts'
import type { FiltrosPedidos } from '../api/types.ts'

export function usePedidos(filtros: FiltrosPedidos, pagina: number, tamanhoPagina: number) {
  return useQuery({
    queryKey: queryKeys.listaPedidos(filtros, pagina, tamanhoPagina),
    queryFn: () => listarPedidos(filtros, pagina, tamanhoPagina),
    // Mantém a página anterior na tela enquanto a próxima carrega.
    placeholderData: keepPreviousData,
  })
}

export function useClientes() {
  return useQuery({ queryKey: queryKeys.clientes, queryFn: listarClientes, staleTime: 5 * 60_000 })
}

export function useProdutos() {
  return useQuery({ queryKey: queryKeys.produtos, queryFn: listarProdutos })
}

export function useCriarPedido() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: criarPedido,
    onSuccess: () =>
      // O pedido novo muda a listagem, e a baixa de estoque muda os produtos.
      Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.pedidos }),
        queryClient.invalidateQueries({ queryKey: queryKeys.produtos }),
      ]),
  })
}
