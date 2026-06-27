import { describe, it, expect, vi, beforeEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useMessagingStore } from '@/stores/useMessagingStore'
import { useAuthStore } from '@/stores/useAuthStore'
import type { ConversationSummary, ConversationDetail, ChatMessage } from '@/types'

const makeConversation = (id = 'c1'): ConversationSummary => ({
  id,
  otherUserId: 'u2',
  otherUserName: 'Bob',
  otherUserAvatarUrl: null,
  lastMessageBody: null,
  lastMessageAt: null,
  unreadCount: 0,
  createdAt: new Date().toISOString(),
})

const makeDetail = (id = 'c1'): ConversationDetail => ({
  id,
  otherUserId: 'u2',
  otherUserName: 'Bob',
  otherUserAvatarUrl: null,
  messages: [],
})

const makeMessage = (convId = 'c1'): ChatMessage => ({
  id: 'm1',
  conversationId: convId,
  senderId: 'u1',
  senderName: 'Alice',
  senderAvatarUrl: null,
  body: 'Hello!',
  isRead: false,
  createdAt: new Date().toISOString(),
})

function mockApiFetch(body: unknown, status = 200) {
  return vi.fn().mockResolvedValue({
    ok: status >= 200 && status < 300,
    status,
    json: () => Promise.resolve(body),
  })
}

beforeEach(() => {
  setActivePinia(createPinia())
  localStorage.clear()
  vi.restoreAllMocks()
})

describe('useMessagingStore', () => {
  describe('fetchConversations', () => {
    it('populates conversations on success', async () => {
      const auth = useAuthStore()
      vi.spyOn(auth, 'apiFetch').mockImplementation(mockApiFetch([makeConversation()]))
      const store = useMessagingStore()

      await store.fetchConversations()

      expect(store.conversations).toHaveLength(1)
      expect(store.error).toBeNull()
    })

    it('sets error on failure', async () => {
      const auth = useAuthStore()
      vi.spyOn(auth, 'apiFetch').mockImplementation(mockApiFetch({}, 500))
      const store = useMessagingStore()

      await store.fetchConversations()

      expect(store.error).toBeTruthy()
      expect(store.isLoading).toBe(false)
    })
  })

  describe('fetchConversation', () => {
    it('sets activeConversation on success', async () => {
      const detail = makeDetail()
      const auth = useAuthStore()
      vi.spyOn(auth, 'apiFetch').mockImplementation(mockApiFetch(detail))
      const store = useMessagingStore()

      await store.fetchConversation('c1')

      expect(store.activeConversation?.id).toBe('c1')
    })

    it('resets activeConversation before loading', async () => {
      const auth = useAuthStore()
      vi.spyOn(auth, 'apiFetch').mockImplementation(mockApiFetch(makeDetail()))
      const store = useMessagingStore()
      store.activeConversation = makeDetail('old')

      const promise = store.fetchConversation('c1')
      expect(store.activeConversation).toBeNull()
      await promise
    })

    it('sets error on failure', async () => {
      const auth = useAuthStore()
      vi.spyOn(auth, 'apiFetch').mockImplementation(mockApiFetch({}, 404))
      const store = useMessagingStore()

      await store.fetchConversation('c1')

      expect(store.error).toBeTruthy()
    })
  })

  describe('getOrCreateConversation', () => {
    it('returns conversation id on success', async () => {
      const auth = useAuthStore()
      vi.spyOn(auth, 'apiFetch').mockImplementation(mockApiFetch(makeConversation('c-new')))
      const store = useMessagingStore()

      const id = await store.getOrCreateConversation('u2')

      expect(id).toBe('c-new')
    })

    it('throws on API error', async () => {
      const auth = useAuthStore()
      vi.spyOn(auth, 'apiFetch').mockImplementation(mockApiFetch({ detail: 'User not found' }, 404))
      const store = useMessagingStore()

      await expect(store.getOrCreateConversation('u-unknown')).rejects.toThrow('User not found')
    })
  })

  describe('sendMessage', () => {
    it('returns message and appends to activeConversation', async () => {
      const msg = makeMessage('c1')
      const auth = useAuthStore()
      vi.spyOn(auth, 'apiFetch').mockImplementation(mockApiFetch(msg))
      const store = useMessagingStore()
      store.activeConversation = makeDetail('c1')

      const result = await store.sendMessage('c1', 'Hello!')

      expect(result.body).toBe('Hello!')
      expect(store.activeConversation?.messages).toHaveLength(1)
    })

    it('does not append to wrong conversation', async () => {
      const msg = makeMessage('c1')
      const auth = useAuthStore()
      vi.spyOn(auth, 'apiFetch').mockImplementation(mockApiFetch(msg))
      const store = useMessagingStore()
      store.activeConversation = makeDetail('c2')

      await store.sendMessage('c1', 'Hello!')

      expect(store.activeConversation?.messages).toHaveLength(0)
    })

    it('throws on API error', async () => {
      const auth = useAuthStore()
      vi.spyOn(auth, 'apiFetch').mockImplementation(mockApiFetch({ detail: 'Forbidden' }, 403))
      const store = useMessagingStore()

      await expect(store.sendMessage('c1', 'Hi')).rejects.toThrow('Forbidden')
    })

    it('sets isSending to false after completion', async () => {
      const auth = useAuthStore()
      vi.spyOn(auth, 'apiFetch').mockImplementation(mockApiFetch(makeMessage()))
      const store = useMessagingStore()

      await store.sendMessage('c1', 'Hi')

      expect(store.isSending).toBe(false)
    })
  })

  describe('fetchUnreadCount', () => {
    it('updates unreadCount from API', async () => {
      const auth = useAuthStore()
      vi.spyOn(auth, 'apiFetch').mockImplementation(mockApiFetch({ count: 5 }))
      const store = useMessagingStore()

      await store.fetchUnreadCount()

      expect(store.unreadCount).toBe(5)
    })

    it('silently ignores errors', async () => {
      const auth = useAuthStore()
      vi.spyOn(auth, 'apiFetch').mockImplementation(mockApiFetch({}, 500))
      const store = useMessagingStore()

      await expect(store.fetchUnreadCount()).resolves.toBeUndefined()
    })
  })

  describe('resetActive', () => {
    it('clears activeConversation and error', () => {
      const store = useMessagingStore()
      store.activeConversation = makeDetail()
      store.error = 'some error'

      store.resetActive()

      expect(store.activeConversation).toBeNull()
      expect(store.error).toBeNull()
    })
  })
})
