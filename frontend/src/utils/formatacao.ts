const moeda = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })
const dataHora = new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'short' })

export function formatarMoeda(valor: number): string {
  return moeda.format(valor)
}

/** A API devolve UTC (ISO com Z); a tela mostra no fuso do navegador. */
export function formatarDataHora(iso: string): string {
  return dataHora.format(new Date(iso))
}
