const baseUrl = import.meta.env.VITE_API_URL ?? ''

export async function apiFetch<T>(
  path: string,
  options: RequestInit = {},
  token?: string | null,
): Promise<T> {
  const headers = new Headers(options.headers)
  headers.set('Content-Type', 'application/json')
  if (token) headers.set('Authorization', `Bearer ${token}`)

  const response = await fetch(`${baseUrl}${path}`, { ...options, headers })

  if (!response.ok) {
    const body = await response.json().catch(() => ({}))
    throw new Error((body as { error?: string }).error ?? response.statusText)
  }

  if (response.status === 204) return undefined as T
  return (await response.json()) as T
}
