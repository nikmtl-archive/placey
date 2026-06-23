<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { MapPin, Users, Search, CalendarDays } from '@lucide/vue'
import DateRangePicker from '@/components/DateRangePicker.vue'

const props = defineProps<{
  initialLocation?: string
  initialCheckIn?: string
  initialCheckOut?: string
  initialGuests?: number
}>()

const emit = defineEmits<{ search: [query: Record<string, string>] }>()

const router = useRouter()

// WHERE (inline input, no popup)
const location = ref(props.initialLocation ?? '')

// WHEN
const checkIn = ref(props.initialCheckIn ?? '')
const checkOut = ref(props.initialCheckOut ?? '')
const pickerRef = ref<InstanceType<typeof DateRangePicker> | null>(null)
const whenActive = ref(false)

function openWhen() {
  closeWho()
  whenActive.value = true
  pickerRef.value?.openPicker()
}

// Track picker open state so the WHEN pill stays highlighted while calendar is open
// We reflect it via whenActive which we set on open and clear on outside-click
// The picker already self-closes; we just need to sync the highlight.
// Use a MutationObserver-free approach: listen to the same mousedown outside logic.

// WHO
const guests = ref(props.initialGuests ?? 2)
const whoActive = ref(false)

function openWho() {
  pickerRef.value?.closePicker()
  whenActive.value = false
  whoActive.value = true
}
function closeWho() {
  whoActive.value = false
}

// SEARCH
const canSearch = computed(
  () => location.value.trim().length > 0 && !!checkIn.value && !!checkOut.value,
)

function handleSearch() {
  if (!canSearch.value) return
  const query: Record<string, string> = {
    location: location.value,
    checkIn: checkIn.value,
    checkOut: checkOut.value,
  }
  if (guests.value > 1) query.guests = String(guests.value)
  whoActive.value = false
  whenActive.value = false
  emit('search', query)
  router.push({ name: 'search', query })
}

// CLICK OUTSIDE
const barRef = ref<HTMLElement | null>(null)
function onClickOutside(e: MouseEvent) {
  if (barRef.value && !barRef.value.contains(e.target as Node)) {
    whoActive.value = false
    whenActive.value = false
  }
}
onMounted(() => document.addEventListener('mousedown', onClickOutside))
onUnmounted(() => document.removeEventListener('mousedown', onClickOutside))

// Date label for WHEN pill
const whenLabel = computed(() => {
  if (checkIn.value && checkOut.value) {
    const f = (s: string) =>
      new Date(s + 'T00:00:00').toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
    return `${f(checkIn.value)} – ${f(checkOut.value)}`
  }
  if (checkIn.value) {
    const f = (s: string) =>
      new Date(s + 'T00:00:00').toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
    return `${f(checkIn.value)} – ?`
  }
  return null
})
</script>

<template>
  <div ref="barRef" class="relative">
    <form
      class="flex items-center bg-white rounded-full shadow-[0_2px_20px_rgba(0,0,0,0.10),0_0_0_1px_rgba(0,0,0,0.05)] transition-shadow pl-3 pr-2 py-2 sm:py-2.5 gap-1"
      :class="{
        'shadow-[0_4px_28px_rgba(0,0,0,0.14),0_0_0_1px_rgba(0,0,0,0.06)]': whoActive || whenActive,
      }"
      @submit.prevent="handleSearch"
    >
      <!-- WHERE — direct inline input -->
      <div class="flex-1 flex items-center gap-2 px-2 sm:px-3 py-1 rounded-full transition-colors min-w-0 cursor-pointer"
        :class="whenActive ? 'bg-gray-50' : 'hover:bg-gray-50'">
        <MapPin class="w-4 h-4 text-muted shrink-0" :stroke-width="2" />
        <div class="flex flex-col min-w-0 flex-1 items-start">
          <span
            class="hidden sm:block text-[10px] font-semibold tracking-widest text-ink uppercase leading-none mb-0.5"
            >WHERE</span
          >
          <input
            v-model="location"
            type="text"
            placeholder="Destination"
            class="w-full bg-transparent text-xs sm:text-sm text-ink placeholder:text-dim outline-none leading-none"
          />
        </div>
      </div>

      <div class="w-px h-7 bg-divider shrink-0 mx-1" />

      <!-- WHEN — pill that opens DateRangePicker -->
      <button
        type="button"
        class="flex-1 flex items-center gap-2 px-2 sm:px-3 py-1 rounded-full transition-colors min-w-0 cursor-pointer"
        :class="whenActive ? 'bg-gray-50' : 'hover:bg-gray-50'"
        @click="openWhen"
      >
        <CalendarDays class="w-4 h-4 text-muted shrink-0" :stroke-width="2" />
        <div class="flex flex-col min-w-0 items-start">
          <span
            class="hidden sm:block text-[10px] font-semibold tracking-widest text-ink uppercase leading-none mb-0.5"
            >WHEN</span
          >
          <span v-if="whenLabel" class="text-xs sm:text-sm text-ink font-medium truncate">{{
            whenLabel
          }}</span>
          <span v-else class="text-xs sm:text-sm text-dim">Add dates</span>
        </div>
      </button>

      <div class="w-px h-7 bg-divider shrink-0 mx-1" />

      <!-- WHO -->
      <button
        type="button"
        class="flex-[0.7] sm:flex-[0.8] flex items-center gap-2 px-2 sm:px-3 py-1 rounded-full transition-colors min-w-0 cursor-pointer"
        :class="whoActive ? 'bg-gray-50' : 'hover:bg-gray-50'"
        @click="openWho"
      >
        <Users class="w-4 h-4 text-muted shrink-0" :stroke-width="2" />
        <div class="flex flex-col min-w-0 items-start">
          <span
            class="hidden sm:block text-[10px] font-semibold tracking-widest text-ink uppercase leading-none mb-0.5"
            >WHO</span
          >
          <span class="text-xs sm:text-sm text-ink font-medium whitespace-nowrap"
            >{{ guests }} {{ guests === 1 ? 'guest' : 'guests' }}</span
          >
        </div>
      </button>

      <!-- Search button -->
      <button
        type="submit"
        class="w-9 h-9 sm:w-11 sm:h-11 rounded-full flex items-center justify-center shrink-0 ml-1 transition-all"
        :class="
          canSearch
            ? 'bg-dark text-white hover:bg-black hover:scale-105 active:scale-95 cursor-pointer'
            : 'bg-gray-200 text-gray-400 cursor-not-allowed'
        "
        aria-label="Search"
      >
        <Search class="w-4 h-4 sm:w-4.5 sm:h-4.5" :stroke-width="2.5" />
      </button>
    </form>

    <!-- DateRangePicker in no-trigger mode — anchored below the WHEN pill -->
    <DateRangePicker
      ref="pickerRef"
      no-trigger
      :check-in="checkIn"
      :check-out="checkOut"
      @update:check-in="checkIn = $event; whenActive = true"
      @update:check-out="checkOut = $event; whenActive = false"
    />

    <!-- WHO popup -->
    <Transition name="popup">
      <div
        v-if="whoActive"
        class="absolute top-[calc(100%+12px)] right-0 w-72 bg-white rounded-2xl shadow-[0_8px_40px_rgba(0,0,0,0.14),0_0_0_1px_rgba(0,0,0,0.06)] z-50 p-5"
      >
        <div class="flex items-center justify-between">
          <div>
            <p class="text-sm font-semibold text-ink">Guests</p>
            <p class="text-xs text-muted mt-0.5">How many guests?</p>
          </div>
          <div class="flex items-center gap-3">
            <button
              type="button"
              class="w-8 h-8 rounded-full border border-divider flex items-center justify-center text-ink hover:border-ink transition-colors text-lg leading-none disabled:opacity-30 disabled:cursor-not-allowed cursor-pointer"
              :disabled="guests <= 1"
              @click="guests = Math.max(1, guests - 1)"
            >
              −
            </button>
            <span class="text-sm font-semibold text-ink w-5 text-center">{{ guests }}</span>
            <button
              type="button"
              class="w-8 h-8 rounded-full border border-divider flex items-center justify-center text-ink hover:border-ink transition-colors text-lg leading-none disabled:opacity-30 disabled:cursor-not-allowed cursor-pointer"
              :disabled="guests >= 20"
              @click="guests = Math.min(20, guests + 1)"
            >
              +
            </button>
          </div>
        </div>
      </div>
    </Transition>
  </div>
</template>

<style scoped>
.popup-enter-active,
.popup-leave-active {
  transition:
    opacity 0.15s ease,
    transform 0.15s ease;
}
.popup-enter-from,
.popup-leave-to {
  opacity: 0;
  transform: translateY(6px);
}
</style>
