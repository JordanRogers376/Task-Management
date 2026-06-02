import AddIcon from '@mui/icons-material/Add'
import CheckCircleIcon from '@mui/icons-material/CheckCircle'
import DeleteIcon from '@mui/icons-material/Delete'
import EditIcon from '@mui/icons-material/Edit'
import SearchIcon from '@mui/icons-material/Search'
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Checkbox,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Fab,
  FormControl,
  IconButton,
  InputAdornment,
  MenuItem,
  Pagination,
  Select,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Tooltip,
  Typography,
  useMediaQuery,
  useTheme,
} from '@mui/material'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import { completeTask, createTask, deleteTask, getTasks, updateTask } from '../api/tasks'
import Layout from '../components/Layout'
import { useAuth } from '../context/AuthContext'
import type { Task } from '../types'

function MobileTaskCard({
  task,
  isAdmin,
  currentUsername,
  selected,
  onSelect,
  onComplete,
  onEdit,
  onDelete,
}: {
  task: Task
  isAdmin: boolean
  currentUsername?: string
  selected: boolean
  onSelect: () => void
  onComplete: () => void
  onEdit: () => void
  onDelete: () => void
}) {
  return (
    <Box
      sx={{
        p: 2,
        borderRadius: 2,
        backgroundColor: task.isCompleted
          ? 'rgba(34, 197, 94, 0.06)'
          : 'rgba(148, 163, 184, 0.04)',
        border: '1px solid',
        borderColor: selected
          ? 'primary.main'
          : task.isCompleted
          ? 'rgba(34, 197, 94, 0.2)'
          : 'rgba(148, 163, 184, 0.1)',
      }}
    >
      <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 1 }}>
        <Checkbox
          checked={selected}
          onChange={onSelect}
          size="small"
          sx={{ mt: -0.5, ml: -0.5 }}
        />
        <Box sx={{ flex: 1 }}>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 1 }}>
            <Typography
              variant="subtitle2"
              sx={{
                fontWeight: 600,
                flex: 1,
                pr: 1,
              }}
            >
              {task.title}
            </Typography>
            <Chip
              label={task.isCompleted ? 'Completed' : 'Pending'}
              size="small"
              sx={{
                height: 24,
                fontSize: '0.7rem',
                backgroundColor: task.isCompleted
                  ? 'rgba(34, 197, 94, 0.15)'
                  : 'rgba(245, 158, 11, 0.15)',
                color: task.isCompleted ? '#22c55e' : '#f59e0b',
                fontWeight: 500,
              }}
            />
          </Box>

          {task.description && (
            <Typography
              variant="body2"
              color="text.secondary"
              sx={{
                mb: 1.5,
                fontSize: '0.85rem',
                overflow: 'hidden',
                textOverflow: 'ellipsis',
                display: '-webkit-box',
                WebkitLineClamp: 2,
                WebkitBoxOrient: 'vertical',
                wordBreak: 'break-word',
              }}
            >
              {task.description}
            </Typography>
          )}

          <Box sx={{ display: 'flex', justifyContent: 'flex-end' }}>
            <Stack direction="row" spacing={0.5}>
              {isAdmin && (
                <Tooltip title="Edit">
                  <IconButton size="small" onClick={onEdit}>
                    <EditIcon sx={{ fontSize: 18 }} />
                  </IconButton>
                </Tooltip>
              )}
              {!task.isCompleted && (isAdmin || task.assignedUsername === currentUsername) && (
                <Tooltip title="Complete">
                  <IconButton size="small" onClick={onComplete} sx={{ color: 'success.main' }}>
                    <CheckCircleIcon sx={{ fontSize: 18 }} />
                  </IconButton>
                </Tooltip>
              )}
              {isAdmin && (
                <Tooltip title="Delete">
                  <IconButton size="small" onClick={onDelete} sx={{ color: 'error.main' }}>
                    <DeleteIcon sx={{ fontSize: 18 }} />
                  </IconButton>
                </Tooltip>
              )}
            </Stack>
          </Box>
        </Box>
      </Box>
    </Box>
  )
}

export default function TasksPage() {
  const theme = useTheme()
  const isMobile = useMediaQuery(theme.breakpoints.down('sm'))
  const { token, user } = useAuth()
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const [createOpen, setCreateOpen] = useState(false)
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [editing, setEditing] = useState<Task | null>(null)
  const [editTitle, setEditTitle] = useState('')
  const [editDescription, setEditDescription] = useState('')
  
  // Pagination state
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  
  // Selection state
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set())

  const isAdmin = user?.role === 'Admin'

  const tasksQuery = useQuery({
    queryKey: ['tasks', token],
    queryFn: () => getTasks(token!),
    enabled: !!token,
  })

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['tasks'] })
    queryClient.invalidateQueries({ queryKey: ['dashboard'] })
  }

  const createMutation = useMutation({
    mutationFn: () => createTask(token!, { title, description: description || null }),
    onSuccess: () => {
      setTitle('')
      setDescription('')
      setCreateOpen(false)
      invalidate()
    },
  })

  const completeMutation = useMutation({
    mutationFn: (id: string) => completeTask(token!, id),
    onSuccess: invalidate,
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteTask(token!, id),
    onSuccess: () => {
      setSelectedIds(new Set())
      invalidate()
    },
  })

  const updateMutation = useMutation({
    mutationFn: () =>
      updateTask(token!, editing!.id, {
        title: editTitle,
        description: editDescription || null,
      }),
    onSuccess: () => {
      setEditing(null)
      invalidate()
    },
  })

  // Filter tasks by search
  const filteredTasks = useMemo(() => {
    if (!tasksQuery.data) return []
    if (!search.trim()) return tasksQuery.data
    const searchLower = search.toLowerCase()
    return tasksQuery.data.filter(
      (task) =>
        task.title.toLowerCase().includes(searchLower) ||
        task.description?.toLowerCase().includes(searchLower)
    )
  }, [tasksQuery.data, search])

  // Paginate tasks
  const totalPages = Math.ceil(filteredTasks.length / pageSize)
  const paginatedTasks = useMemo(() => {
    const start = (page - 1) * pageSize
    return filteredTasks.slice(start, start + pageSize)
  }, [filteredTasks, page, pageSize])

  // Reset to page 1 when search changes
  useMemo(() => {
    setPage(1)
  }, [search])

  // Selection handlers
  const handleSelectAll = () => {
    if (selectedIds.size === paginatedTasks.length) {
      setSelectedIds(new Set())
    } else {
      setSelectedIds(new Set(paginatedTasks.map((t) => t.id)))
    }
  }

  const handleSelectOne = (id: string) => {
    const newSelected = new Set(selectedIds)
    if (newSelected.has(id)) {
      newSelected.delete(id)
    } else {
      newSelected.add(id)
    }
    setSelectedIds(newSelected)
  }

  const isAllSelected = paginatedTasks.length > 0 && selectedIds.size === paginatedTasks.length
  const isIndeterminate = selectedIds.size > 0 && selectedIds.size < paginatedTasks.length

  // Bulk actions
  const handleBulkDelete = async () => {
    for (const id of selectedIds) {
      await deleteMutation.mutateAsync(id)
    }
    setSelectedIds(new Set())
  }

  const handleBulkComplete = async () => {
    for (const id of selectedIds) {
      const task = paginatedTasks.find((t) => t.id === id)
      if (task && !task.isCompleted) {
        await completeMutation.mutateAsync(id)
      }
    }
    setSelectedIds(new Set())
  }

  return (
    <Layout>
      <Box sx={{ maxWidth: 1200, mx: 'auto' }}>
        {/* Sticky Header */}
        <Box
          sx={{
            position: 'sticky',
            top: 0,
            zIndex: 10,
            backgroundColor: 'background.default',
            height: 79,
            display: 'flex',
            alignItems: 'center',
            mb: 3,
            borderBottom: '1px solid rgba(148, 163, 184, 0.1)',
            mx: { xs: -2, md: -3 },
            px: { xs: 2, md: 3 },
          }}
        >
          <Typography variant={isMobile ? 'h5' : 'h4'} sx={{ fontWeight: 700 }}>
            Task List
          </Typography>
        </Box>

        {/* Search Field + Create Button */}
        <Box
          sx={{
            mb: 2,
            display: 'flex',
            gap: 2,
            flexDirection: { xs: 'column', sm: 'row' },
            alignItems: { xs: 'stretch', sm: 'center' },
          }}
        >
          <TextField
            placeholder="Search tasks..."
            size="small"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            InputProps={{
              startAdornment: (
                <InputAdornment position="start">
                  <SearchIcon sx={{ color: 'text.secondary' }} />
                </InputAdornment>
              ),
            }}
            sx={{
              flex: 1,
              maxWidth: { sm: 300 },
              '& .MuiOutlinedInput-root': {
                backgroundColor: 'rgba(148, 163, 184, 0.04)',
              },
            }}
          />
          {isAdmin && !isMobile && (
            <Button
              variant="contained"
              startIcon={<AddIcon />}
              onClick={() => setCreateOpen(true)}
              sx={{
                background: 'linear-gradient(135deg, #818cf8, #6366f1)',
                '&:hover': {
                  background: 'linear-gradient(135deg, #a5b4fc, #818cf8)',
                },
              }}
            >
              Create Task
            </Button>
          )}
        </Box>

        {/* Bulk Actions */}
        {selectedIds.size > 0 && (
          <Box
            sx={{
              mb: 2,
              p: 1.5,
              borderRadius: 2,
              backgroundColor: 'rgba(129, 140, 248, 0.1)',
              display: 'flex',
              alignItems: 'center',
              gap: 2,
            }}
          >
            <Typography variant="body2" sx={{ fontWeight: 500 }}>
              {selectedIds.size} selected
            </Typography>
            <Button
              size="small"
              variant="outlined"
              color="success"
              onClick={handleBulkComplete}
              startIcon={<CheckCircleIcon />}
            >
              Complete
            </Button>
            {isAdmin && (
              <Button
                size="small"
                variant="outlined"
                color="error"
                onClick={handleBulkDelete}
                startIcon={<DeleteIcon />}
              >
                Delete
              </Button>
            )}
          </Box>
        )}

        {/* Tasks */}
        <Card>
          {tasksQuery.isError && (
            <Alert severity="error" sx={{ m: 2 }}>
              Failed to load tasks
            </Alert>
          )}

          {/* Mobile: Card-based layout */}
          {isMobile ? (
            <CardContent sx={{ p: 2 }}>
              {paginatedTasks.length === 0 && !tasksQuery.isLoading ? (
                <Box sx={{ py: 4, textAlign: 'center' }}>
                  <Typography variant="body2" color="text.secondary">
                    {search ? 'No matching tasks' : 'No tasks yet'}
                  </Typography>
                </Box>
              ) : (
                <Stack spacing={1.5}>
                  {paginatedTasks.map((task) => (
                    <MobileTaskCard
                      key={task.id}
                      task={task}
                      isAdmin={isAdmin}
                      currentUsername={user?.username}
                      selected={selectedIds.has(task.id)}
                      onSelect={() => handleSelectOne(task.id)}
                      onComplete={() => completeMutation.mutate(task.id)}
                      onEdit={() => {
                        setEditing(task)
                        setEditTitle(task.title)
                        setEditDescription(task.description ?? '')
                      }}
                      onDelete={() => deleteMutation.mutate(task.id)}
                    />
                  ))}
                </Stack>
              )}
            </CardContent>
          ) : (
            /* Desktop: Table layout */
            <TableContainer>
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell padding="checkbox">
                      <Checkbox
                        checked={isAllSelected}
                        indeterminate={isIndeterminate}
                        onChange={handleSelectAll}
                      />
                    </TableCell>
                    <TableCell sx={{ fontWeight: 600 }}>Title</TableCell>
                    <TableCell sx={{ fontWeight: 600 }}>Description</TableCell>
                    <TableCell sx={{ fontWeight: 600 }}>Status</TableCell>
                    <TableCell sx={{ fontWeight: 600 }}>Actions</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {paginatedTasks.length === 0 && !tasksQuery.isLoading && (
                    <TableRow>
                      <TableCell colSpan={5} sx={{ py: 6, textAlign: 'center' }}>
                        <Typography color="text.secondary">
                          {search ? 'No matching tasks' : 'No tasks yet'}
                        </Typography>
                      </TableCell>
                    </TableRow>
                  )}
                  {paginatedTasks.map((task) => (
                    <TableRow
                      key={task.id}
                      selected={selectedIds.has(task.id)}
                      sx={{
                        backgroundColor: task.isCompleted
                          ? 'rgba(34, 197, 94, 0.06)'
                          : 'transparent',
                        '&:hover': {
                          backgroundColor: task.isCompleted
                            ? 'rgba(34, 197, 94, 0.1)'
                            : 'rgba(148, 163, 184, 0.04)',
                        },
                        '&.Mui-selected': {
                          backgroundColor: 'rgba(129, 140, 248, 0.08)',
                          '&:hover': {
                            backgroundColor: 'rgba(129, 140, 248, 0.12)',
                          },
                        },
                      }}
                    >
                      <TableCell padding="checkbox">
                        <Checkbox
                          checked={selectedIds.has(task.id)}
                          onChange={() => handleSelectOne(task.id)}
                        />
                      </TableCell>
                      <TableCell>
                        <Typography variant="body2" sx={{ fontWeight: 500 }}>
                          {task.title}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Typography
                          variant="body2"
                          color="text.secondary"
                          sx={{
                            maxWidth: 300,
                            overflow: 'hidden',
                            textOverflow: 'ellipsis',
                            whiteSpace: 'nowrap',
                          }}
                        >
                          {task.description || '—'}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Chip
                          label={task.isCompleted ? 'Completed' : 'Pending'}
                          size="small"
                          sx={{
                            backgroundColor: task.isCompleted
                              ? 'rgba(34, 197, 94, 0.15)'
                              : 'rgba(245, 158, 11, 0.15)',
                            color: task.isCompleted ? '#22c55e' : '#f59e0b',
                            fontWeight: 500,
                          }}
                        />
                      </TableCell>
                      <TableCell>
                        <Stack direction="row" spacing={0.5}>
                          {isAdmin && (
                            <Tooltip title="Edit">
                              <IconButton
                                size="small"
                                onClick={() => {
                                  setEditing(task)
                                  setEditTitle(task.title)
                                  setEditDescription(task.description ?? '')
                                }}
                              >
                                <EditIcon fontSize="small" />
                              </IconButton>
                            </Tooltip>
                          )}
                          {!task.isCompleted && (isAdmin || task.assignedUsername === user?.username) && (
                            <Tooltip title="Complete">
                              <IconButton
                                size="small"
                                onClick={() => completeMutation.mutate(task.id)}
                                sx={{ color: 'success.main' }}
                              >
                                <CheckCircleIcon fontSize="small" />
                              </IconButton>
                            </Tooltip>
                          )}
                          {isAdmin && (
                            <Tooltip title="Delete">
                              <IconButton
                                size="small"
                                onClick={() => deleteMutation.mutate(task.id)}
                                sx={{ color: 'error.main' }}
                              >
                                <DeleteIcon fontSize="small" />
                              </IconButton>
                            </Tooltip>
                          )}
                        </Stack>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          )}

          {/* Pagination */}
          {filteredTasks.length > 0 && (
            <Box
              sx={{
                p: 2,
                borderTop: '1px solid rgba(148, 163, 184, 0.1)',
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'center',
                flexWrap: 'wrap',
                gap: 2,
              }}
            >
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <Pagination
                  count={totalPages}
                  page={page}
                  onChange={(_, newPage) => setPage(newPage)}
                  size={isMobile ? 'small' : 'medium'}
                  sx={{
                    '& .MuiPaginationItem-root': {
                      color: 'text.secondary',
                      '&.Mui-selected': {
                        backgroundColor: 'primary.main',
                        color: 'white',
                      },
                    },
                  }}
                />
              </Box>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <FormControl size="small">
                  <Select
                    value={pageSize}
                    onChange={(e) => {
                      setPageSize(Number(e.target.value))
                      setPage(1)
                    }}
                    sx={{
                      minWidth: 100,
                      '& .MuiSelect-select': {
                        py: 0.75,
                      },
                    }}
                  >
                    <MenuItem value={5}>5 / page</MenuItem>
                    <MenuItem value={10}>10 / page</MenuItem>
                    <MenuItem value={25}>25 / page</MenuItem>
                    <MenuItem value={50}>50 / page</MenuItem>
                    <MenuItem value={100}>100 / page</MenuItem>
                  </Select>
                </FormControl>
              </Box>
            </Box>
          )}
        </Card>
      </Box>

      {/* Create Dialog */}
      <Dialog
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        fullWidth
        maxWidth="sm"
        fullScreen={isMobile}
        PaperProps={{ sx: { backgroundColor: 'background.paper' } }}
      >
        <DialogTitle sx={{ fontWeight: 600 }}>Create Task</DialogTitle>
        <DialogContent>
          <TextField
            label="Title"
            fullWidth
            margin="normal"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            autoFocus
          />
          <TextField
            label="Description"
            fullWidth
            margin="normal"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            multiline
            rows={3}
          />
        </DialogContent>
        <DialogActions sx={{ p: 3, pt: 0 }}>
          <Button onClick={() => setCreateOpen(false)}>Cancel</Button>
          <Button
            variant="contained"
            onClick={() => createMutation.mutate()}
            disabled={!title || createMutation.isPending}
          >
            Create
          </Button>
        </DialogActions>
      </Dialog>

      {/* Edit Dialog */}
      <Dialog
        open={!!editing}
        onClose={() => setEditing(null)}
        fullWidth
        maxWidth="sm"
        fullScreen={isMobile}
        PaperProps={{ sx: { backgroundColor: 'background.paper' } }}
      >
        <DialogTitle sx={{ fontWeight: 600 }}>Edit Task</DialogTitle>
        <DialogContent>
          <TextField
            label="Title"
            fullWidth
            margin="normal"
            value={editTitle}
            onChange={(e) => setEditTitle(e.target.value)}
          />
          <TextField
            label="Description"
            fullWidth
            margin="normal"
            value={editDescription}
            onChange={(e) => setEditDescription(e.target.value)}
            multiline
            rows={3}
          />
        </DialogContent>
        <DialogActions sx={{ p: 3, pt: 0 }}>
          <Button onClick={() => setEditing(null)}>Cancel</Button>
          <Button
            variant="contained"
            onClick={() => updateMutation.mutate()}
            disabled={updateMutation.isPending}
          >
            Save
          </Button>
        </DialogActions>
      </Dialog>

      {/* Mobile FAB for Create Task */}
      {isAdmin && (
        <Fab
          color="primary"
          onClick={() => setCreateOpen(true)}
          sx={{
            position: 'fixed',
            bottom: 24,
            right: 24,
            display: { xs: 'flex', md: 'none' },
            background: 'linear-gradient(135deg, #818cf8, #6366f1)',
          }}
        >
          <AddIcon />
        </Fab>
      )}
    </Layout>
  )
}
