import type { IconName } from '../components/Icon'

export interface ReactionMeta {
  type: number
  label: string
  emoji: string
  color: string
}

// Order matches Fakebook.Server/Domain/Enums.cs ReactionType.
export const REACTIONS: ReactionMeta[] = [
  { type: 0, label: 'Like', emoji: '👍', color: '#1877f2' },
  { type: 1, label: 'Love', emoji: '❤️', color: '#f3425f' },
  { type: 2, label: 'Haha', emoji: '😆', color: '#f7b125' },
  { type: 3, label: 'Wow', emoji: '😮', color: '#f7b125' },
  { type: 4, label: 'Sad', emoji: '😢', color: '#f7b125' },
  { type: 5, label: 'Angry', emoji: '😡', color: '#e9710f' },
]

export function reactionMeta(type: number | null | undefined): ReactionMeta | null {
  if (type == null) return null
  return REACTIONS.find((r) => r.type === type) ?? null
}

export interface PrivacyMeta {
  value: number
  label: string
  icon: IconName
}

export const PRIVACY: PrivacyMeta[] = [
  { value: 0, label: 'Public', icon: 'globe' },
  { value: 1, label: 'Friends', icon: 'friends' },
  { value: 2, label: 'Only me', icon: 'lock' },
]

export function privacyMeta(value: number): PrivacyMeta {
  return PRIVACY.find((p) => p.value === value) ?? PRIVACY[0]
}

export function initials(name: string): string {
  const parts = name.trim().split(/\s+/).slice(0, 2)
  return parts.map((p) => p.charAt(0).toUpperCase()).join('') || 'F'
}

export function timeAgo(value: string): string {
  const then = new Date(value).getTime()
  const minutes = Math.floor((Date.now() - then) / 60000)
  if (Number.isNaN(minutes)) return ''
  if (minutes < 1) return 'Just now'
  if (minutes < 60) return `${minutes}m`
  const hours = Math.floor(minutes / 60)
  if (hours < 24) return `${hours}h`
  const days = Math.floor(hours / 24)
  if (days < 7) return `${days}d`
  const weeks = Math.floor(days / 7)
  if (weeks < 5) return `${weeks}w`
  return new Date(value).toLocaleDateString()
}

export function firstName(name: string): string {
  return name.trim().split(/\s+/)[0] || name
}
