<script setup lang="ts">
import { MessageCircle } from '@lucide/vue'
import type { ConversationSummary } from '@/types'

defineProps<{
  conversations: ConversationSummary[]
  isLoading: boolean
  error: string | null
  activeId: string | undefined
}>()

const emit = defineEmits<{
  select: [id: string]
  retry: []
}>()

function formatDate(iso: string | null): string {
  if (!iso) return ''
  const d = new Date(iso)
  const now = new Date()
  const diffDays = Math.floor((now.getTime() - d.getTime()) / 86_400_000)
  if (diffDays === 0) return d.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' })
  if (diffDays < 7) return d.toLocaleDateString('en-US', { weekday: 'short' })
  return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
}

function initials(name: string): string {
  return name.split(' ').map((w) => w[0]).join('').toUpperCase().slice(0, 2)
}
</script>

<template>
  <!-- Loading skeleton -->
  <div v-if="isLoading" class="space-y-2">
    <div
      v-for="i in 5"
      :key="i"
      class="animate-pulse flex items-center gap-4 px-4 py-4 rounded-2xl bg-white border border-divider"
    >
      <div class="w-11 h-11 rounded-full bg-divider-light shrink-0" />
      <div class="flex-1 space-y-2">
        <div class="h-3.5 w-1/3 bg-divider-light rounded" />
        <div class="h-3 w-2/3 bg-divider rounded" />
      </div>
    </div>
  </div>

  <!-- Error -->
  <div v-else-if="error" class="text-center py-16">
    <p class="text-sm text-muted mb-3">{{ error }}</p>
    <button class="text-sm font-medium text-ink underline underline-offset-2 cursor-pointer" @click="emit('retry')">
      Try again
    </button>
  </div>

  <!-- Empty -->
  <div v-else-if="conversations.length === 0" class="flex flex-col items-center justify-center py-20 text-center">
    <div class="w-16 h-16 rounded-full bg-cream flex items-center justify-center mx-auto mb-4">
      <MessageCircle :size="26" class="text-ink/25" />
    </div>
    <p class="font-serif text-xl text-ink mb-1">No messages yet</p>
    <p class="text-sm text-muted max-w-xs leading-relaxed">
      Start a conversation by messaging a host on any listing.
    </p>
  </div>

  <!-- List -->
  <div
    v-else
    class="divide-y divide-divider-light border border-divider rounded-3xl bg-white overflow-hidden shadow-[0_1px_8px_rgba(0,0,0,0.05)]"
  >
    <button
      v-for="conv in conversations"
      :key="conv.id"
      class="w-full flex items-center gap-4 px-5 py-4 transition-colors text-left cursor-pointer"
      :class="conv.id === activeId ? 'bg-cream' : 'hover:bg-black/[0.025]'"
      @click="emit('select', conv.id)"
    >
      <!-- Avatar -->
      <div class="relative shrink-0">
        <div class="w-11 h-11 rounded-full bg-cream border border-divider-light flex items-center justify-center select-none overflow-hidden">
          <img
            v-if="conv.otherUserAvatarUrl"
            :src="conv.otherUserAvatarUrl"
            :alt="conv.otherUserName"
            class="w-full h-full object-cover"
          />
          <span v-else class="text-sm font-semibold text-ink/50">
            {{ initials(conv.otherUserName) }}
          </span>
        </div>
        <span
          v-if="conv.unreadCount > 0"
          class="absolute -top-0.5 -right-0.5 w-3.5 h-3.5 bg-ink rounded-full border-2 border-white"
        />
      </div>

      <!-- Content -->
      <div class="flex-1 min-w-0">
        <div class="flex items-baseline justify-between gap-2 mb-0.5">
          <p
            class="text-sm truncate"
            :class="conv.unreadCount > 0 ? 'font-bold text-ink' : 'font-semibold text-ink'"
          >
            {{ conv.otherUserName }}
          </p>
          <span class="text-[11px] text-muted shrink-0">{{ formatDate(conv.lastMessageAt) }}</span>
        </div>
        <div class="flex items-center justify-between gap-2">
          <p
            class="text-xs truncate"
            :class="conv.unreadCount > 0 ? 'text-ink font-medium' : 'text-muted'"
          >
            {{ conv.lastMessageBody ?? 'No messages yet' }}
          </p>
          <span
            v-if="conv.unreadCount > 0"
            class="bg-ink text-white text-[10px] font-bold rounded-full min-w-[18px] h-[18px] flex items-center justify-center px-1 shrink-0"
          >
            {{ conv.unreadCount }}
          </span>
        </div>
      </div>
    </button>
  </div>
</template>
