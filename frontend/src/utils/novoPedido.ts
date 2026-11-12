import type { CriarPedidoRequest, Produto } from '../api/types.ts'

/** Linha do formulário. Os valores ficam como texto, iguais aos dos inputs. */
export interface LinhaItem {
  chave: number
  produtoId: string
  quantidade: string
}

export interface ValidacaoNovoPedido {
  erros: string[]
  pedido: CriarPedidoRequest | null
}

let proximaChave = 1
export function novaLinha(): LinhaItem {
  return { chave: proximaChave++, produtoId: '', quantidade: '1' }
}

function quantidadeValida(texto: string): number | null {
  const valor = Number(texto)
  return Number.isInteger(valor) && valor > 0 ? valor : null
}

/** Total exibido na tela. O valor oficial é o que a API devolve (RN-10). */
export function calcularTotalEstimado(linhas: LinhaItem[], produtos: Map<number, Produto>): number {
  return linhas.reduce((total, linha) => {
    const produto = produtos.get(Number(linha.produtoId))
    const quantidade = quantidadeValida(linha.quantidade)
    return produto && quantidade ? total + produto.preco * quantidade : total
  }, 0)
}

/**
 * Validação na tela, para dar retorno imediato. A API valida tudo de novo
 * (RN-01 a RN-08) e é a autoridade final, inclusive sobre o estoque.
 */
export function validarNovoPedido(
  clienteId: string,
  linhas: LinhaItem[],
  produtos: Map<number, Produto>,
): ValidacaoNovoPedido {
  const erros: string[] = []
  if (!clienteId) erros.push('Selecione o cliente.')

  const itens: CriarPedidoRequest['itens'] = []
  linhas.forEach((linha, indice) => {
    const rotulo = `Item ${indice + 1}`
    const produto = produtos.get(Number(linha.produtoId))
    if (!produto) {
      erros.push(`${rotulo}: selecione um produto.`)
      return
    }
    const quantidade = quantidadeValida(linha.quantidade)
    if (!quantidade) {
      erros.push(`${rotulo}: informe uma quantidade inteira maior que zero.`)
      return
    }
    if (quantidade > produto.estoque) {
      erros.push(`${rotulo}: há apenas ${produto.estoque} unidade(s) de "${produto.nome}" em estoque.`)
      return
    }
    itens.push({ produtoId: produto.id, quantidade })
  })

  if (linhas.length === 0) erros.push('Adicione ao menos um item.')

  return erros.length > 0 ? { erros, pedido: null } : { erros, pedido: { clienteId: Number(clienteId), itens } }
}
