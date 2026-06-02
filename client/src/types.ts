export type LoginResponse = {
  token: string
  expiresAt: string
  username: string
  role: string
  tenantId: string
  tenantName: string
}

export type Task = {
  id: string
  title: string
  description: string | null
  isCompleted: boolean
  createdDate: string
  completedAt: string | null
  assignedUserId: string
  assignedUsername: string
}

export type DashboardSummary = {
  totalTasks: number
  completedTasks: number
  pendingTasks: number
}

export type CreateTaskRequest = {
  title: string
  description?: string | null
  assignedUserId?: string | null
}

export type UpdateTaskRequest = {
  title: string
  description?: string | null
  assignedUserId?: string | null
}
