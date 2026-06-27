<script setup lang="ts">
import { ref, computed, onMounted, nextTick, watch } from 'vue'
import { storeToRefs } from 'pinia'
import { useMessagingStore, useAuthStore } from '@/stores'
import { ArrowLeft, Send } from '@lucide/vue'

const props = defineProps<{
  conversationId: string
}>()

const emit = defineEmits<{
  back: []
}>()

const authStore = useAuthStore()
const messagingStore = useMessagingStore()
const { activeConversation, isLoading, isSending, error } = storeToRefs(messagingStore)

const newMessage = ref('')
const messagesEndRef = ref<HTMLElement | null>(null)
const textareaRef = ref<HTMLTextAreaElement | null>(null)

const myId = computed(() => authStore.user?.id)

onMounted(async () => {
  messagingStore.resetActive()
  await messagingStore.fetchConversation(props.conversationId)
  scrollToBottom()
})

watch(
  () => activeConversation.value?.messages.length,
  async () => {
    await nextTick()
    scrollToBottom()
  },
)

function scrollToBottom() {
  messagesEndRef.value?.scrollIntoView({ behavior: 'smooth' })
}

async function send() {
  const body = newMessage.value.trim()
  if (!body || isSending.value) return
  newMessage.value = ''
  resetTextareaHeight()
  try {
    await messagingStore.sendMessage(props.conversationId, body)
  } catch {
    newMessage.value = body
  }
}

function handleKeydown(e: KeyboardEvent) {
  if (e.key === 'Enter' && !e.shiftKey) {
    e.preventDefault()
    send()
  }
}

function autoResize(e: Event) {
  const el = e.target as HTMLTextAreaElement
  el.style.height = 'auto'
  el.style.height = Math.min(el.scrollHeight, 128) + 'px'
}

function resetTextareaHeight() {
  if (textareaRef.value) textareaRef.value.style.height = 'auto'
}

function formatTime(iso: string): string {
  return new Date(iso).toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' })
}

function initials(name: string): string {
  return name
    .split(' ')
    .map((w) => w[0])
    .join('')
    .toUpperCase()
    .slice(0, 2)
}
</script>

<template>
  <div
    class="flex flex-col border border-divider rounded-3xl bg-white overflow-hidden shadow-[0_1px_8px_rgba(0,0,0,0.05)]"
    style="height: calc(100dvh - 12rem)"
  >
    <!-- Header -->
    <div class="flex items-center gap-3 px-5 py-4 border-b border-divider shrink-0">
      <button
        class="md:hidden p-1.5 rounded-xl hover:bg-black/5 transition-colors cursor-pointer text-ink/50 hover:text-ink"
        aria-label="Back to messages"
        @click="emit('back')"
      >
        <ArrowLeft :size="18" />
      </button>

      <template v-if="activeConversation">
        <div
          class="w-9 h-9 rounded-full bg-cream border border-divider-light flex items-center justify-center shrink-0 overflow-hidden"
        >
          <img
            v-if="activeConversation.otherUserAvatarUrl"
            :src="activeConversation.otherUserAvatarUrl"
            :alt="activeConversation.otherUserName"
            class="w-full h-full object-cover"
          />
          <span v-else class="text-xs font-semibold text-ink/50 select-none">
            {{ initials(activeConversation.otherUserName) }}
          </span>
        </div>
        <p class="text-sm font-semibold text-ink">{{ activeConversation.otherUserName }}</p>
      </template>

      <div v-else-if="isLoading" class="flex items-center gap-3">
        <div class="w-9 h-9 rounded-full bg-divider-light animate-pulse" />
        <div class="h-3.5 w-32 bg-divider-light rounded animate-pulse" />
      </div>
    </div>

    <!-- Loading -->
    <div v-if="isLoading && !activeConversation" class="flex-1 flex items-center justify-center">
      <div class="space-y-2.5 w-full max-w-sm px-6">
        <div v-for="i in 4" :key="i" class="animate-pulse rounded-2xl bg-divider-light h-10" />
      </div>
    </div>

    <!-- Error -->
    <div
      v-else-if="error && !activeConversation"
      class="flex-1 flex flex-col items-center justify-center gap-3 p-6 text-center"
    >
      <p class="text-sm text-muted">{{ error }}</p>
      <button
        class="text-sm font-medium text-ink underline underline-offset-2 cursor-pointer"
        @click="emit('back')"
      >
        Go back
      </button>
    </div>

    <!-- Messages -->
    <template v-else-if="activeConversation">
      <div class="flex-1 overflow-y-auto px-6 py-5 flex flex-col gap-1 bg-page-bg">
        <div
          v-if="activeConversation.messages.length === 0"
          class="flex-1 flex flex-col items-center justify-center text-center py-16"
        >
          <p class="font-serif text-xl text-ink mb-1">Start the conversation</p>
          <p class="text-sm text-muted">Say hello to {{ activeConversation.otherUserName }}</p>
        </div>

        <template v-else>
          <div
            v-for="(msg, i) in activeConversation.messages"
            :key="msg.id"
            class="flex flex-col"
            :class="msg.senderId === myId ? 'items-end' : 'items-start'"
          >
            <!-- Date separator -->
            <div
              v-if="
                i === 0 ||
                new Date(msg.createdAt).toDateString() !==
                  new Date(activeConversation.messages[i - 1]!.createdAt).toDateString()
              "
              class="self-center text-[10px] text-muted bg-white border border-divider px-3 py-0.5 rounded-full my-4"
            >
              {{
                new Date(msg.createdAt).toLocaleDateString('en-US', {
                  weekday: 'short',
                  month: 'short',
                  day: 'numeric',
                })
              }}
            </div>

            <div
              class="max-w-[72%] px-4 py-2.5 rounded-2xl text-sm leading-relaxed"
              :class="
                msg.senderId === myId
                  ? 'bg-ink text-white rounded-br-sm'
                  : 'bg-white border border-divider text-ink rounded-bl-sm shadow-[0_1px_4px_rgba(0,0,0,0.06)]'
              "
            >
              {{ msg.body }}
            </div>
            <span class="text-[10px] text-muted/60 mt-1 px-1">{{ formatTime(msg.createdAt) }}</span>
          </div>
        </template>

        <div ref="messagesEndRef" />
      </div>

      <!-- Input bar -->
      <div class="border-t border-divider px-5 py-3.5 flex items-end gap-3 bg-white shrink-0">
        <textarea
          ref="textareaRef"
          v-model="newMessage"
          placeholder="Message…"
          rows="1"
          class="flex-1 resize-none border border-divider rounded-2xl px-4 py-2.5 text-sm text-ink placeholder:text-muted/50 focus:outline-none focus:border-ink/30 transition-colors bg-page-bg max-h-32 leading-relaxed"
          @keydown="handleKeydown"
          @input="autoResize"
        />
        <button
          class="w-10 h-10 rounded-full flex items-center justify-center transition-colors shrink-0 mb-0.5"
          :class="
            newMessage.trim() && !isSending
              ? 'bg-ink text-white hover:bg-black cursor-pointer'
              : 'bg-divider-light text-muted cursor-not-allowed'
          "
          :disabled="!newMessage.trim() || isSending"
          aria-label="Send message"
          @click="send"
        >
          <Send :size="15" />
        </button>
      </div>
    </template>
  </div>
</template>
