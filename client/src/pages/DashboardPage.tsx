import AssignmentTurnedInIcon from '@mui/icons-material/AssignmentTurnedIn'
import ExpandMoreIcon from '@mui/icons-material/ExpandMore'
import PendingActionsIcon from '@mui/icons-material/PendingActions'
import TaskIcon from '@mui/icons-material/Task'
import {
  Avatar,
  Box,
  Card,
  CardContent,
  Chip,
  Collapse,
  Stack,
  Typography,
  useMediaQuery,
  useTheme,
} from '@mui/material'
import { useQuery } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import { getDashboardSummary, getTasks } from '../api/tasks'
import Layout from '../components/Layout'
import { useAuth } from '../context/AuthContext'

type FilterType = 'all' | 'completed' | 'pending'

interface StatCardProps {
  title: string
  value: number | string
  icon: React.ReactNode
  color: string
  gradient: string
  compact?: boolean
  selected?: boolean
  onClick?: () => void
}

function StatCard({ title, value, icon, color, gradient, compact, selected, onClick }: StatCardProps) {
  return (
    <Card
      onClick={onClick}
      sx={{
        position: 'relative',
        overflow: 'hidden',
        cursor: onClick ? 'pointer' : 'default',
        transition: 'all 0.2s ease',
        outline: selected ? `2px solid ${color}` : 'none',
        outlineOffset: -2,
        '&:hover': onClick ? {
          transform: 'translateY(-2px)',
          boxShadow: `0 8px 24px ${color}25`,
        } : {},
        '&::before': {
          content: '""',
          position: 'absolute',
          top: 0,
          left: 0,
          right: 0,
          height: 3,
          background: gradient,
        },
      }}
    >
      <CardContent sx={{ p: compact ? 2 : 3, '&:last-child': { pb: compact ? 2 : 3 } }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
          <Box>
            <Typography
              variant="caption"
              color="text.secondary"
              sx={{ mb: 0.5, display: 'block', fontSize: compact ? '0.7rem' : '0.75rem' }}
            >
              {title}
            </Typography>
            <Typography variant={compact ? 'h4' : 'h3'} sx={{ fontWeight: 700 }}>
              {value}
            </Typography>
          </Box>
          <Avatar
            sx={{
              width: compact ? 44 : 56,
              height: compact ? 44 : 56,
              bgcolor: `${color}15`,
              color: color,
            }}
          >
            {icon}
          </Avatar>
        </Box>
      </CardContent>
    </Card>
  )
}

interface TaskItemProps {
  task: {
    id: string
    title: string
    description?: string | null
    isCompleted: boolean
  }
  expanded: boolean
  onToggle: () => void
}

function TaskItem({ task, expanded, onToggle }: TaskItemProps) {
  return (
    <Box
      onClick={onToggle}
      sx={{
        p: 2,
        borderRadius: 2,
        backgroundColor: task.isCompleted
          ? 'rgba(34, 197, 94, 0.06)'
          : 'rgba(148, 163, 184, 0.04)',
        border: '1px solid',
        borderColor: task.isCompleted
          ? 'rgba(34, 197, 94, 0.2)'
          : 'rgba(148, 163, 184, 0.1)',
        cursor: 'pointer',
        transition: 'all 0.2s ease',
        '&:hover': {
          backgroundColor: task.isCompleted
            ? 'rgba(34, 197, 94, 0.1)'
            : 'rgba(148, 163, 184, 0.08)',
        },
      }}
    >
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flex: 1 }}>
          <ExpandMoreIcon
            sx={{
              color: 'text.secondary',
              transition: 'transform 0.2s ease',
              transform: expanded ? 'rotate(180deg)' : 'rotate(0deg)',
            }}
          />
          <Typography variant="subtitle2" sx={{ fontWeight: 600 }}>
            {task.title}
          </Typography>
        </Box>
        <Chip
          label={task.isCompleted ? 'Completed' : 'Pending'}
          size="small"
          sx={{
            height: 22,
            fontSize: '0.7rem',
            backgroundColor: task.isCompleted
              ? 'rgba(34, 197, 94, 0.15)'
              : 'rgba(245, 158, 11, 0.15)',
            color: task.isCompleted ? '#22c55e' : '#f59e0b',
            fontWeight: 500,
          }}
        />
      </Box>
      <Collapse in={expanded}>
        <Box sx={{ mt: 2, pl: 4 }}>
          {task.description ? (
            <Typography
              variant="body2"
              color="text.secondary"
              sx={{
                wordBreak: 'break-word',
                whiteSpace: 'pre-wrap',
              }}
            >
              {task.description}
            </Typography>
          ) : (
            <Typography variant="body2" color="text.secondary" sx={{ fontStyle: 'italic' }}>
              No description
            </Typography>
          )}
        </Box>
      </Collapse>
    </Box>
  )
}

export default function DashboardPage() {
  const theme = useTheme()
  const isMobile = useMediaQuery(theme.breakpoints.down('sm'))
  const { token } = useAuth()
  const [filter, setFilter] = useState<FilterType>('all')
  const [expandedId, setExpandedId] = useState<string | null>(null)

  const summaryQuery = useQuery({
    queryKey: ['dashboard', token],
    queryFn: () => getDashboardSummary(token!),
    enabled: !!token,
  })

  const tasksQuery = useQuery({
    queryKey: ['tasks', token],
    queryFn: () => getTasks(token!),
    enabled: !!token,
  })

  const filteredTasks = useMemo(() => {
    if (!tasksQuery.data) return []
    switch (filter) {
      case 'completed':
        return tasksQuery.data.filter((t) => t.isCompleted)
      case 'pending':
        return tasksQuery.data.filter((t) => !t.isCompleted)
      default:
        return tasksQuery.data
    }
  }, [tasksQuery.data, filter])

  const handleCardClick = (type: FilterType) => {
    setFilter(type)
    setExpandedId(null)
  }

  const handleTaskToggle = (id: string) => {
    setExpandedId(expandedId === id ? null : id)
  }

  const getFilterLabel = () => {
    switch (filter) {
      case 'all':
        return 'All Tasks'
      case 'completed':
        return 'Completed Tasks'
      case 'pending':
        return 'Pending Tasks'
      default:
        return ''
    }
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
            Tenant Task Dashboard
          </Typography>
        </Box>

        {/* Stats Grid - 3 cards */}
        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: { xs: '1fr', sm: 'repeat(3, 1fr)' },
            gap: { xs: 2, md: 3 },
            mb: 3,
          }}
        >
          <StatCard
            title="Total Tasks"
            value={summaryQuery.data?.totalTasks ?? 0}
            icon={<TaskIcon fontSize={isMobile ? 'medium' : 'large'} />}
            color="#818cf8"
            gradient="linear-gradient(135deg, #818cf8, #6366f1)"
            compact={isMobile}
            selected={filter === 'all'}
            onClick={() => handleCardClick('all')}
          />
          <StatCard
            title="Completed Tasks"
            value={summaryQuery.data?.completedTasks ?? 0}
            icon={<AssignmentTurnedInIcon fontSize={isMobile ? 'medium' : 'large'} />}
            color="#22c55e"
            gradient="linear-gradient(135deg, #22c55e, #16a34a)"
            compact={isMobile}
            selected={filter === 'completed'}
            onClick={() => handleCardClick('completed')}
          />
          <StatCard
            title="Pending Tasks"
            value={summaryQuery.data?.pendingTasks ?? 0}
            icon={<PendingActionsIcon fontSize={isMobile ? 'medium' : 'large'} />}
            color="#f59e0b"
            gradient="linear-gradient(135deg, #f59e0b, #d97706)"
            compact={isMobile}
            selected={filter === 'pending'}
            onClick={() => handleCardClick('pending')}
          />
        </Box>

        {/* Tasks List - Always visible */}
        <Card>
          <CardContent sx={{ p: 0 }}>
            <Box
              sx={{
                p: 2,
                borderBottom: '1px solid rgba(148, 163, 184, 0.1)',
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'center',
              }}
            >
              <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
                {getFilterLabel()}
              </Typography>
              <Chip
                label={`${filteredTasks.length} tasks`}
                size="small"
                sx={{ backgroundColor: 'rgba(148, 163, 184, 0.1)' }}
              />
            </Box>

            <Box sx={{ p: 2 }}>
              {tasksQuery.isLoading ? (
                <Typography variant="body2" color="text.secondary" sx={{ py: 2, textAlign: 'center' }}>
                  Loading tasks...
                </Typography>
              ) : filteredTasks.length === 0 ? (
                <Typography variant="body2" color="text.secondary" sx={{ py: 2, textAlign: 'center' }}>
                  No tasks found
                </Typography>
              ) : (
                <Stack spacing={1.5}>
                  {filteredTasks.map((task) => (
                    <TaskItem
                      key={task.id}
                      task={task}
                      expanded={expandedId === task.id}
                      onToggle={() => handleTaskToggle(task.id)}
                    />
                  ))}
                </Stack>
              )}
            </Box>
          </CardContent>
        </Card>
      </Box>
    </Layout>
  )
}
