import { useState, type FormEvent } from 'react'
import type { Cliente, FiltrosPedidos } from '../api/types.ts'

interface FiltrosProps {
  clientes: Cliente[]
  clientesIndisponiveis: boolean
  onAplicar: (filtros: FiltrosPedidos) => void
}

export function Filtros({ clientes, clientesIndisponiveis, onAplicar }: FiltrosProps) {
  const [clienteId, setClienteId] = useState('')
  const [dataInicio, setDataInicio] = useState('')
  const [dataFim, setDataFim] = useState('')
  const [erro, setErro] = useState<string | null>(null)

  function aplicar(evento: FormEvent<HTMLFormElement>) {
    evento.preventDefault()
    // yyyy-MM-dd compara corretamente como texto.
    if (dataInicio && dataFim && dataInicio > dataFim) {
      setErro('A data inicial não pode ser depois da data final.')
      return
    }
    setErro(null)
    onAplicar({
      clienteId: clienteId ? Number(clienteId) : undefined,
      dataInicio: dataInicio || undefined,
      dataFim: dataFim || undefined,
    })
  }

  function limpar() {
    setClienteId('')
    setDataInicio('')
    setDataFim('')
    setErro(null)
    onAplicar({})
  }

  return (
    <form className="filtros cartao" onSubmit={aplicar} aria-label="Filtros de pedidos">
      <div className="campo">
        <label htmlFor="filtro-cliente">Cliente</label>
        <select
          id="filtro-cliente"
          value={clienteId}
          onChange={(e) => setClienteId(e.target.value)}
          disabled={clientesIndisponiveis}
        >
          <option value="">Todos</option>
          {clientes.map((cliente) => (
            <option key={cliente.id} value={cliente.id}>
              {cliente.nome} ({cliente.email})
            </option>
          ))}
        </select>
      </div>
      <div className="campo">
        <label htmlFor="filtro-inicio">Data inicial</label>
        <input id="filtro-inicio" type="date" value={dataInicio} onChange={(e) => setDataInicio(e.target.value)} />
      </div>
      <div className="campo">
        <label htmlFor="filtro-fim">Data final</label>
        <input id="filtro-fim" type="date" value={dataFim} onChange={(e) => setDataFim(e.target.value)} />
      </div>
      <div className="filtros__acoes">
        <button type="submit" className="botao botao--primario">
          Filtrar
        </button>
        <button type="button" className="botao" onClick={limpar}>
          Limpar
        </button>
      </div>
      {erro && (
        <p className="filtros__erro" role="alert">
          {erro}
        </p>
      )}
    </form>
  )
}
