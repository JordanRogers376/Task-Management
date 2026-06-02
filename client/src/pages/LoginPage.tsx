import VisibilityIcon from '@mui/icons-material/Visibility'
import VisibilityOffIcon from '@mui/icons-material/VisibilityOff'
import {
  Alert,
  Box,
  Button,
  IconButton,
  InputAdornment,
  TextField,
  Typography,
  useMediaQuery,
  useTheme,
} from '@mui/material'
import { useMutation } from '@tanstack/react-query'
import { useState } from 'react'
import { Navigate } from 'react-router-dom'
import { login } from '../api/tasks'
import { useAuth } from '../context/AuthContext'

export default function LoginPage() {
  const theme = useTheme()
  const isMobile = useMediaQuery(theme.breakpoints.down('md'))
  const { token, login: saveAuth } = useAuth()
  const [username, setUsername] = useState('admin@acme.com')
  const [password, setPassword] = useState('Password123!')
  const [showPassword, setShowPassword] = useState(false)

  const mutation = useMutation({
    mutationFn: () => login(username, password),
    onSuccess: saveAuth,
  })

  if (token) return <Navigate to="/" replace />

  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'flex',
        flexDirection: { xs: 'column', md: 'row' },
        background: 'linear-gradient(135deg, #0f172a 0%, #1e293b 50%, #0f172a 100%)',
      }}
    >
      {/* Left side - Branding (hidden on mobile, shown as header) */}
      <Box
        sx={{
          flex: { md: 1 },
          display: 'flex',
          flexDirection: 'column',
          justifyContent: { xs: 'flex-start', md: 'center' },
          alignItems: 'center',
          p: { xs: 3, md: 6 },
          pt: { xs: 4, md: 6 },
          position: 'relative',
          overflow: 'hidden',
        }}
      >
        {/* Background decoration - only on desktop */}
        {!isMobile && (
          <>
            <Box
              sx={{
                position: 'absolute',
                width: 500,
                height: 500,
                borderRadius: '50%',
                background: 'radial-gradient(circle, rgba(99, 102, 241, 0.15) 0%, transparent 70%)',
                top: '20%',
                left: '10%',
              }}
            />
            <Box
              sx={{
                position: 'absolute',
                width: 300,
                height: 300,
                borderRadius: '50%',
                background: 'radial-gradient(circle, rgba(34, 211, 238, 0.1) 0%, transparent 70%)',
                bottom: '20%',
                right: '20%',
              }}
            />
          </>
        )}

        <Box sx={{ position: 'relative', textAlign: 'center', maxWidth: 480 }}>
          <Box
            sx={{
              width: { xs: 56, md: 80 },
              height: { xs: 56, md: 80 },
              borderRadius: { xs: 3, md: 4 },
              background: 'linear-gradient(135deg, #818cf8 0%, #6366f1 100%)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              fontWeight: 700,
              fontSize: { xs: 24, md: 36 },
              mb: { xs: 2, md: 4 },
              mx: 'auto',
              boxShadow: '0 20px 40px rgba(99, 102, 241, 0.3)',
            }}
          >
            T
          </Box>
          <Typography variant={isMobile ? 'h5' : 'h3'} sx={{ fontWeight: 700, mb: { xs: 1, md: 2 } }}>
            TaskFlow
          </Typography>

          {/* Extended description only on desktop */}
          {!isMobile && (
            <>
              <Typography variant="h6" sx={{ color: 'text.secondary', fontWeight: 400, lineHeight: 1.6 }}>
                Multi-tenant task management platform. Streamline your workflow, collaborate with your team, and get things done.
              </Typography>
              <Box sx={{ mt: 6, display: 'flex', gap: 4, justifyContent: 'center' }}>
                {['Multi-tenant', 'Role-based', 'Real-time'].map((feature) => (
                  <Box key={feature} sx={{ textAlign: 'center' }}>
                    <Box
                      sx={{
                        width: 12,
                        height: 12,
                        borderRadius: '50%',
                        bgcolor: 'primary.main',
                        mx: 'auto',
                        mb: 1,
                      }}
                    />
                    <Typography variant="caption" color="text.secondary">
                      {feature}
                    </Typography>
                  </Box>
                ))}
              </Box>
            </>
          )}
        </Box>
      </Box>

      {/* Right side - Login form */}
      <Box
        sx={{
          width: { xs: '100%', md: 440 },
          display: 'flex',
          flexDirection: 'column',
          justifyContent: { xs: 'flex-start', md: 'center' },
          p: { xs: 3, sm: 4, md: 6 },
          flex: { xs: 1, md: 'none' },
        }}
      >
        <Box sx={{ maxWidth: 400, mx: 'auto', width: '100%' }}>
          <Box sx={{ mb: 3 }}>
            <Typography variant={isMobile ? 'h5' : 'h4'} sx={{ fontWeight: 700, mb: 0.5 }}>
              Welcome back
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Sign in to continue to your workspace
            </Typography>
          </Box>

          {mutation.isError && (
            <Alert
              severity="error"
              sx={{
                mb: 2.5,
                borderRadius: 2,
                backgroundColor: 'rgba(239, 68, 68, 0.1)',
                border: '1px solid rgba(239, 68, 68, 0.2)',
                '& .MuiAlert-message': { fontSize: '0.875rem' },
              }}
            >
              {mutation.error instanceof Error ? mutation.error.message : 'Login failed'}
            </Alert>
          )}

          <Box component="form" onSubmit={(e) => { e.preventDefault(); mutation.mutate() }}>
            <TextField
              label="Username"
              fullWidth
              size={isMobile ? 'small' : 'medium'}
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              required
              sx={{ mb: 2 }}
              InputProps={{
                sx: { backgroundColor: 'rgba(148, 163, 184, 0.04)' },
              }}
            />
            <TextField
              label="Password"
              type={showPassword ? 'text' : 'password'}
              fullWidth
              size={isMobile ? 'small' : 'medium'}
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              sx={{ mb: 2.5 }}
              InputProps={{
                sx: { backgroundColor: 'rgba(148, 163, 184, 0.04)' },
                endAdornment: (
                  <InputAdornment position="end">
                    <IconButton
                      onClick={() => setShowPassword(!showPassword)}
                      edge="end"
                      size="small"
                    >
                      {showPassword ? <VisibilityOffIcon fontSize="small" /> : <VisibilityIcon fontSize="small" />}
                    </IconButton>
                  </InputAdornment>
                ),
              }}
            />
            <Button
              type="submit"
              variant="contained"
              fullWidth
              size={isMobile ? 'medium' : 'large'}
              disabled={mutation.isPending}
              sx={{
                py: isMobile ? 1.25 : 1.5,
                fontSize: isMobile ? '0.9rem' : '1rem',
                background: 'linear-gradient(135deg, #818cf8 0%, #6366f1 100%)',
                '&:hover': {
                  background: 'linear-gradient(135deg, #a5b4fc 0%, #818cf8 100%)',
                },
              }}
            >
              {mutation.isPending ? 'Signing in...' : 'Sign in'}
            </Button>
          </Box>

          <Box
            sx={{
              mt: 3,
              p: 2,
              borderRadius: 2,
              backgroundColor: 'rgba(148, 163, 184, 0.06)',
              border: '1px solid rgba(148, 163, 184, 0.1)',
            }}
          >
            <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 0.5 }}>
              Demo credentials:
            </Typography>
            <Typography variant="caption" sx={{ fontFamily: 'monospace', display: 'block', fontSize: '0.7rem' }}>
              admin@acme.com · Password123!
            </Typography>
            <Typography variant="caption" sx={{ fontFamily: 'monospace', display: 'block', fontSize: '0.7rem' }}>
              user@acme.com · Password123!
            </Typography>
          </Box>
        </Box>
      </Box>
    </Box>
  )
}
