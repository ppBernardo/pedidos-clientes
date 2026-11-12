import type { ProblemDetails } from './types.ts'

const BASE_URL = '/api'

/** Erro de chamada à API, com o ProblemDetails devolvido pelo backend. */
export class ApiError extends Error {
  readonly status: number
  readonly problema: ProblemDetails | null

  constructor(status: number, problema: ProblemDetails | null) {
    super(mensagemPara(status, problema))
    this.name = 'ApiError'
    this.status = status
    this.problema = problema
  }

  /** Mensagens de validação (400) por campo, prontas para exibir. */
  get errosDeValidacao(): string[] {
    const erros = this.problema?.errors
    return erros ? Object.values(erros).flat() : []
  }
}

function mensagemPara(status: number, problema: ProblemDetails | null): string {
  if (problema?.detail) return problema.detail
  if (problema?.errors) return 'Alguns campos estão inválidos.'
  if (problema?.title) return problema.title
  if (status === 0) return 'Não foi possível conectar à API.'
  return `A API respondeu com erro ${status}.`
}

async function lerProblema(resposta: Response): Promise<ProblemDetails | null> {
  const tipo = resposta.headers.get('content-type') ?? ''
  if (!tipo.includes('json')) return null
  try {
    return (await resposta.json()) as ProblemDetails
  } catch {
    return null
  }
}

/** Único ponto de acesso HTTP do frontend. */
export async function requisicao<T>(caminho: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers)
  headers.set('Accept', 'application/json')
  if (init.body) headers.set('Content-Type', 'application/json')

  let resposta: Response
  try {
    resposta = await fetch(`${BASE_URL}${caminho}`, { ...init, headers })
  } catch {
    throw new ApiError(0, null)
  }

  if (!resposta.ok) throw new ApiError(resposta.status, await lerProblema(resposta))
  return (await resposta.json()) as T
}
