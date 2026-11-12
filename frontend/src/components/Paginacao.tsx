interface PaginacaoProps {
  pagina: number
  totalPaginas: number
  totalItens: number
  onMudarPagina: (pagina: number) => void
}

export function Paginacao({ pagina, totalPaginas, totalItens, onMudarPagina }: PaginacaoProps) {
  const ultima = Math.max(totalPaginas, 1)
  return (
    <nav className="paginacao" aria-label="Paginação">
      <span className="paginacao__info">
        Página {pagina} de {ultima} · {totalItens} {totalItens === 1 ? 'pedido' : 'pedidos'}
      </span>
      <div className="paginacao__botoes">
        <button type="button" className="botao" onClick={() => onMudarPagina(pagina - 1)} disabled={pagina <= 1}>
          Anterior
        </button>
        <button type="button" className="botao" onClick={() => onMudarPagina(pagina + 1)} disabled={pagina >= ultima}>
          Próxima
        </button>
      </div>
    </nav>
  )
}
