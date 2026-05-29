export type LoginResponse = {
  token: string
  expiresAt: string
  email: string
  role: string
  tenantId: string
  tenantName: string
}

export type Task = {
  id: string
  title: string
  description: string | null
  isCompleted: boolean
  createdAt: string
  completedAt: string | null
  createdByEmail: string
}

export type CreateTaskRequest = {
  title: string
  description?: string | null
}

export type UpdateTaskRequest = {
  title: string
  description?: string | null
}
