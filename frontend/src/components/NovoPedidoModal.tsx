import { useEffect, useMemo, useRef, useState, type FormEvent } from 'react'
import { ApiError } from '../api/client.ts'
import type { Cliente, PedidoDetalhe, Produto } from '../api/types.ts'
import { useCriarPedido, useProdutos } from '../hooks/usePedidos.ts'
import { formatarMoeda } from '../utils/formatacao.ts'
import { calcularTotalEstimado, novaLinha, validarNovoPedido, type LinhaItem } from '../utils/novoPedido.ts'

interface NovoPedidoModalProps {
  clientes: Cliente[]
  onFechar: () => void
  onCriado: (pedido: PedidoDetalhe) => void
}

/** Montado só enquanto aberto: cada abertura começa com o formulário limpo. */
export function NovoPedidoModal({ clientes, onFechar, onCriado }: NovoPedidoModalProps) {
  const dialogo = useRef<HTMLDialogElement>(null)
  const produtos = useProdutos()
  const criar = useCriarPedido()

  const [clienteId, setClienteId] = useState('')
  const [linhas, setLinhas] = useState<LinhaItem[]>(() => [novaLinha()])
  const [errosLocais, setErrosLocais] = useState<string[]>([])

  useEffect(() => {
    const elemento = dialogo.current
    if (elemento && !elemento.open) elemento.showModal()
  }, [])

  const produtosPorId = useMemo(
    () => new Map<number, Produto>((produtos.data ?? []).map((p) => [p.id, p])),
    [produtos.data],
  )
  const total = calcularTotalEstimado(linhas, produtosPorId)

  function alterarLinha(chave: number, mudanca: Partial<LinhaItem>) {
    setLinhas((atuais) => atuais.map((l) => (l.chave === chave ? { ...l, ...mudanca } : l)))
  }

  function enviar(evento: FormEvent<HTMLFormElement>) {
    evento.preventDefault()
    const { erros, pedido } = validarNovoPedido(clienteId, linhas, produtosPorId)
    setErrosLocais(erros)
    if (!pedido) return
    criar.mutate(pedido, { onSuccess: onCriado })
  }

  const erroApi = criar.error instanceof ApiError ? criar.error : null
  const produtosEmOutrasLinhas = (chave: number) =>
    new Set(linhas.filter((l) => l.chave !== chave && l.produtoId).map((l) => l.produtoId))

  return (
    <dialog ref={dialogo} className="modal" aria-labelledby="titulo-novo-pedido" onClose={onFechar}>
      <form onSubmit={enviar} noValidate>
        <header className="modal__cabecalho">
          <h2 id="titulo-novo-pedido">Novo pedido</h2>
        </header>

        <div className="modal__corpo">
          <div className="campo">
            <label htmlFor="novo-cliente">Cliente</label>
            <select id="novo-cliente" value={clienteId} onChange={(e) => setClienteId(e.target.value)} required>
              <option value="">Selecione o cliente</option>
              {clientes.map((cliente) => (
                <option key={cliente.id} value={cliente.id}>
                  {cliente.nome} ({cliente.email})
                </option>
              ))}
            </select>
          </div>

          <fieldset className="itens">
            <legend>Itens</legend>
            {produtos.isPending && <p className="estado">Carregando produtos…</p>}
            {produtos.isError && (
              <p className="estado estado--erro" role="alert">
                Não foi possível carregar os produtos: {produtos.error.message}
              </p>
            )}
            {linhas.map((linha, indice) => {
              const usados = produtosEmOutrasLinhas(linha.chave)
              const produto = produtosPorId.get(Number(linha.produtoId))
              return (
                <div className="item" key={linha.chave}>
                  <div className="campo item__produto">
                    <label htmlFor={`produto-${linha.chave}`}>Produto {indice + 1}</label>
                    <select
                      id={`produto-${linha.chave}`}
                      value={linha.produtoId}
                      onChange={(e) => alterarLinha(linha.chave, { produtoId: e.target.value })}
                    >
                      <option value="">Selecione um produto</option>
                      {(produtos.data ?? []).map((p) => (
                        <option key={p.id} value={p.id} disabled={usados.has(String(p.id)) || p.estoque === 0}>
                          {p.nome}: {formatarMoeda(p.preco)} · {p.estoque > 0 ? `${p.estoque} em estoque` : 'sem estoque'}
                        </option>
                      ))}
                    </select>
                  </div>
                  <div className="campo item__quantidade">
                    <label htmlFor={`quantidade-${linha.chave}`}>Quantidade</label>
                    <input
                      id={`quantidade-${linha.chave}`}
                      type="number"
                      min={1}
                      max={produto?.estoque}
                      step={1}
                      value={linha.quantidade}
                      onChange={(e) => alterarLinha(linha.chave, { quantidade: e.target.value })}
                    />
                  </div>
                  <button
                    type="button"
                    className="botao item__remover"
                    onClick={() => setLinhas((atuais) => atuais.filter((l) => l.chave !== linha.chave))}
                    disabled={linhas.length === 1}
                    aria-label={`Remover item ${indice + 1}`}
                  >
                    Remover
                  </button>
                </div>
              )
            })}
            <button type="button" className="botao" onClick={() => setLinhas((atuais) => [...atuais, novaLinha()])}>
              Adicionar item
            </button>
          </fieldset>

          <p className="modal__total">
            Total estimado: <strong>{formatarMoeda(total)}</strong>
          </p>

          {errosLocais.length > 0 && (
            <ul className="alerta alerta--erro" role="alert">
              {errosLocais.map((erro) => (
                <li key={erro}>{erro}</li>
              ))}
            </ul>
          )}
          {erroApi && (
            <div className="alerta alerta--erro" role="alert">
              <p>{erroApi.message}</p>
              {erroApi.errosDeValidacao.length > 0 && (
                <ul>
                  {erroApi.errosDeValidacao.map((erro) => (
                    <li key={erro}>{erro}</li>
                  ))}
                </ul>
              )}
            </div>
          )}
        </div>

        <footer className="modal__rodape">
          <button type="button" className="botao" onClick={() => dialogo.current?.close()}>
            Cancelar
          </button>
          <button type="submit" className="botao botao--primario" disabled={criar.isPending}>
            {criar.isPending ? 'Salvando…' : 'Criar pedido'}
          </button>
        </footer>
      </form>
    </dialog>
  )
}
