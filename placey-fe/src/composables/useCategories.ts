import type { Category } from '@/types'

export const CATEGORIES: Category[] = [
  { id: 'Beach',       label: 'BEACH',       icon: '🏖️' },
  { id: 'Mountains',   label: 'MOUNTAINS',   icon: '⛰️' },
  { id: 'Urban',       label: 'URBAN',       icon: '🏙️' },
  { id: 'Luxe',        label: 'LUXE',        icon: '💎' },
  { id: 'Countryside', label: 'COUNTRYSIDE', icon: '🌿' },
  { id: 'Island',      label: 'ISLAND',      icon: '🏝️' },
  { id: 'Lake',        label: 'LAKE',        icon: '🏞️' },
]

export function useCategories() {
  return { categories: CATEGORIES }
}
