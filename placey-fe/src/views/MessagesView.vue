<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { storeToRefs } from 'pinia'
import { useMessagingStore } from '@/stores'
import ConversationList from '@/components/ConversationList.vue'
import ConversationView from '@/components/ConversationView.vue'

const router = useRouter()
const route = useRoute()
const messagingStore = useMessagingStore()
const { conversations, isLoading, error } = storeToRefs(messagingStore)

onMounted(() => messagingStore.fetchConversations())

const activeId = computed(() => route.params.id as string | undefined)

function openConversation(id: string) {
  router.push({ name: 'messages', params: { id } })
}
</script>

<template>
  <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-10 py-10 lg:py-14">
    <!-- Page heading -->
    <div class="mb-8">
      <h1 class="font-serif text-3xl font-semibold text-ink mb-1">Messages</h1>
      <p class="text-sm text-muted">Your conversations with hosts and guests</p>
    </div>

    <!-- Two-panel layout -->
    <div class="flex gap-6 items-start">
      <!-- Left: conversation list -->
      <div
        class="shrink-0 w-full md:w-85 lg:w-95"
        :class="activeId ? 'hidden md:block' : 'block'"
      >
        <ConversationList
          :conversations="conversations"
          :is-loading="isLoading"
          :error="error"
          :active-id="activeId"
          @select="openConversation"
          @retry="messagingStore.fetchConversations()"
        />
      </div>

      <!-- Right: open conversation or placeholder -->
      <div class="flex-1 min-w-0" :class="activeId ? 'block' : 'hidden md:block'">
        <ConversationView v-if="activeId" :key="activeId" :conversation-id="activeId" @back="router.push({ name: 'messages' })" />

        <!-- No conversation selected placeholder -->
        <div
          v-else
          class="flex flex-col items-center justify-center text-center py-28 border border-dashed border-divider rounded-3xl bg-white"
        >
          <p class="font-serif text-xl text-ink mb-1">Select a conversation</p>
          <p class="text-sm text-muted">Choose one from the list to read and reply.</p>
        </div>
      </div>
    </div>
  </div>
</template>
