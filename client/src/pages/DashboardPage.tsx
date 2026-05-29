import { useEffect, useState, type FormEvent } from 'react'
import { completeTask, createTask, deleteTask, getTasks, updateTask } from '../api/tasks'
import { useAuth } from '../context/AuthContext'
import type { Task } from '../types'

export default function DashboardPage() {
  const { token, user, logout } = useAuth()
  const [tasks, setTasks] = useState<Task[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [editingId, setEditingId] = useState<string | null>(null)
  const [editTitle, setEditTitle] = useState('')
  const [editDescription, setEditDescription] = useState('')

  async function loadTasks() {
    if (!token) return
    setLoading(true)
    setError('')
    try {
      setTasks(await getTasks(token))
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load tasks')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadTasks()
  }, [token])

  async function handleCreate(e: FormEvent) {
    e.preventDefault()
    if (!token) return
    await createTask(token, { title, description: description || null })
    setTitle('')
    setDescription('')
    await loadTasks()
  }

  async function handleComplete(id: string) {
    if (!token) return
    await completeTask(token, id)
    await loadTasks()
  }

  async function handleDelete(id: string) {
    if (!token) return
    await deleteTask(token, id)
    await loadTasks()
  }

  function startEdit(task: Task) {
    setEditingId(task.id)
    setEditTitle(task.title)
    setEditDescription(task.description ?? '')
  }

  async function handleUpdate(e: FormEvent) {
    e.preventDefault()
    if (!token || !editingId) return
    await updateTask(token, editingId, {
      title: editTitle,
      description: editDescription || null,
    })
    setEditingId(null)
    await loadTasks()
  }

  const isAdmin = user?.role === 'Admin'

  return (
    <div className="page">
      <header className="header">
        <div>
          <h1>{user?.tenantName}</h1>
          <p className="muted">
            {user?.email} · {user?.role}
          </p>
        </div>
        <button className="secondary" onClick={logout}>
          Sign out
        </button>
      </header>

      <section className="card">
        <h2>New task</h2>
        <form className="inline-form" onSubmit={handleCreate}>
          <input
            placeholder="Title"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            required
          />
          <input
            placeholder="Description (optional)"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
          />
          <button type="submit">Add</button>
        </form>
      </section>

      <section className="card">
        <h2>Tasks</h2>
        {error && <p className="error">{error}</p>}
        {loading ? (
          <p className="muted">Loading...</p>
        ) : tasks.length === 0 ? (
          <p className="muted">No tasks yet.</p>
        ) : (
          <ul className="task-list">
            {tasks.map((task) => (
              <li key={task.id} className={task.isCompleted ? 'completed' : ''}>
                {editingId === task.id ? (
                  <form className="edit-form" onSubmit={handleUpdate}>
                    <input value={editTitle} onChange={(e) => setEditTitle(e.target.value)} required />
                    <input
                      value={editDescription}
                      onChange={(e) => setEditDescription(e.target.value)}
                      placeholder="Description"
                    />
                    <div className="actions">
                      <button type="submit">Save</button>
                      <button type="button" className="secondary" onClick={() => setEditingId(null)}>
                        Cancel
                      </button>
                    </div>
                  </form>
                ) : (
                  <>
                    <div>
                      <strong>{task.title}</strong>
                      {task.description && <p>{task.description}</p>}
                      <p className="muted">
                        {task.createdByEmail} · {new Date(task.createdAt).toLocaleString()}
                      </p>
                    </div>
                    <div className="actions">
                      {!task.isCompleted && (
                        <button onClick={() => handleComplete(task.id)}>Complete</button>
                      )}
                      <button className="secondary" onClick={() => startEdit(task)}>
                        Edit
                      </button>
                      {isAdmin && (
                        <button className="danger" onClick={() => handleDelete(task.id)}>
                          Delete
                        </button>
                      )}
                    </div>
                  </>
                )}
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  )
}
