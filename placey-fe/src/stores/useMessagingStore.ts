import { defineStore } from 'pinia'
import { ref } from 'vue'
import { useAuthStore } from './useAuthStore'
import type { ConversationSummary, ConversationDetail, ChatMessage } from '@/types'

export const useMessagingStore = defineStore('messaging', () => {
  const authStore = useAuthStore()

  const conversations = ref<ConversationSummary[]>([])
  const activeConversation = ref<ConversationDetail | null>(null)
  const unreadCount = ref(0)
  const isLoading = ref(false)
  const isSending = ref(false)
  const error = ref<string | null>(null)

  let _eventSource: EventSource | null = null

  async function fetchConversations() {
    isLoading.value = true
    error.value = null
    try {
      const res = await authStore.apiFetch('/api/messages')
      if (!res.ok) throw new Error(`HTTP ${res.status}`)
      conversations.value = await res.json()
    } catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to load messages'
    } finally {
      isLoading.value = false
    }
  }

  async function fetchConversation(id: string) {
    isLoading.value = true
    error.value = null
    activeConversation.value = null
    try {
      const res = await authStore.apiFetch(`/api/messages/conversations/${id}`)
      if (!res.ok) throw new Error(`HTTP ${res.status}`)
      activeConversation.value = await res.json()
      await fetchUnreadCount()
    } catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to load conversation'
    } finally {
      isLoading.value = false
    }
  }

  async function getOrCreateConversation(otherUserId: string): Promise<string> {
    const res = await authStore.apiFetch('/api/messages/conversations', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ otherUserId }),
    })
    if (!res.ok) {
      const body = await res.json().catch(() => ({}))
      throw new Error(body.detail ?? body.message ?? `HTTP ${res.status}`)
    }
    const data: ConversationSummary = await res.json()
    return data.id
  }

  async function sendMessage(conversationId: string, body: string): Promise<ChatMessage> {
    isSending.value = true
    try {
      const res = await authStore.apiFetch(
        `/api/messages/conversations/${conversationId}/messages`,
        {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ body }),
        },
      )
      if (!res.ok) {
        const err = await res.json().catch(() => ({}))
        throw new Error(err.detail ?? err.message ?? `HTTP ${res.status}`)
      }
      const msg: ChatMessage = await res.json()
      if (activeConversation.value?.id === conversationId) {
        activeConversation.value.messages.push(msg)
      }
      return msg
    } finally {
      isSending.value = false
    }
  }

  async function fetchUnreadCount() {
    try {
      const res = await authStore.apiFetch('/api/messages/unread-count')
      if (!res.ok) return
      const data = await res.json()
      unreadCount.value = data.count
    } catch {
      // silently ignore
    }
  }

  function connectStream() {
    if (_eventSource) return
    const token = authStore.accessToken
    if (!token) return

    _eventSource = new EventSource(`/api/messages/stream?access_token=${token}`)

    _eventSource.onmessage = (e) => {
      const msg: ChatMessage = JSON.parse(e.data)

      if (activeConversation.value?.id === msg.conversationId) {
        activeConversation.value.messages.push(msg)
      } else {
        unreadCount.value++
        const conv = conversations.value.find((c) => c.id === msg.conversationId)
        if (conv) {
          conv.lastMessageBody = msg.body
          conv.lastMessageAt = msg.createdAt
          conv.unreadCount++
        }
      }
    }

    _eventSource.onerror = () => {
      _eventSource?.close()
      _eventSource = null
      setTimeout(connectStream, 5000)
    }
  }

  function disconnectStream() {
    _eventSource?.close()
    _eventSource = null
  }

  function resetActive() {
    activeConversation.value = null
    error.value = null
  }

  return {
    conversations,
    activeConversation,
    unreadCount,
    isLoading,
    isSending,
    error,
    fetchConversations,
    fetchConversation,
    getOrCreateConversation,
    sendMessage,
    fetchUnreadCount,
    connectStream,
    disconnectStream,
    resetActive,
  }
})
