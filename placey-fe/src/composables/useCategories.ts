import { WavesLadder, MountainSnow, Building2, Gem, Sprout, TreePalm, Droplets } from '@lucide/vue'
import type { Category } from '@/types'

export const CATEGORIES: Category[] = [
  { id: 'Beach',       label: 'Beach',       icon: WavesLadder  },
  { id: 'Mountains',   label: 'Mountains',   icon: MountainSnow },
  { id: 'Urban',       label: 'Urban',       icon: Building2    },
  { id: 'Luxe',        label: 'Luxe',        icon: Gem          },
  { id: 'Countryside', label: 'Countryside', icon: Sprout       },
  { id: 'Island',      label: 'Island',      icon: TreePalm     },
  { id: 'Lake',        label: 'Lake',        icon: Droplets     },
]

export function useCategories() {
  return { categories: CATEGORIES }
}
