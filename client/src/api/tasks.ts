import type { CreateTaskRequest, DashboardSummary, LoginResponse, Task, UpdateTaskRequest } from '../types'
import { apiFetch } from './client'

export function login(username: string, password: string) {
  return apiFetch<LoginResponse>('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify({ username, password }),
  })
}

export function getTasks(token: string) {
  return apiFetch<Task[]>('/api/tasks', {}, token)
}

export function getDashboardSummary(token: string) {
  return apiFetch<DashboardSummary>('/api/dashboard/summary', {}, token)
}

export function createTask(token: string, request: CreateTaskRequest) {
  return apiFetch<Task>('/api/tasks', {
    method: 'POST',
    body: JSON.stringify(request),
  }, token)
}

export function updateTask(token: string, id: string, request: UpdateTaskRequest) {
  return apiFetch<Task>(`/api/tasks/${id}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  }, token)
}

export function completeTask(token: string, id: string) {
  return apiFetch<Task>(`/api/tasks/${id}/complete`, { method: 'PATCH' }, token)
}

export function deleteTask(token: string, id: string) {
  return apiFetch<void>(`/api/tasks/${id}`, { method: 'DELETE' }, token)
}
