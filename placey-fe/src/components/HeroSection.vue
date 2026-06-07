<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { MapPin, Calendar, Users, Search, ChevronLeft, ChevronRight } from '@lucide/vue'

const router = useRouter()

// ── WHERE ─────────────────────────────────────────────────────────
const location = ref('')
const activeSection = ref<'where' | 'when' | 'who' | null>(null)

// ── WHEN calendar ─────────────────────────────────────────────────
const checkIn = ref<Date | null>(null)
const checkOut = ref<Date | null>(null)
const hoverDate = ref<Date | null>(null)
const calendarMonth = ref(new Date().getMonth())
const calendarYear = ref(new Date().getFullYear())
const selectingCheckout = ref(false)

const MONTHS = ['January','February','March','April','May','June',
                'July','August','September','October','November','December']
const DAYS   = ['Su','Mo','Tu','We','Th','Fr','Sa']

const nextMonth = computed(() => calendarMonth.value === 11 ? 0 : calendarMonth.value + 1)
const nextYear  = computed(() => calendarMonth.value === 11 ? calendarYear.value + 1 : calendarYear.value)

function calendarDays(month: number, year: number) {
  const first = new Date(year, month, 1).getDay()
  const total = new Date(year, month + 1, 0).getDate()
  const days: (number | null)[] = Array(first).fill(null)
  for (let d = 1; d <= total; d++) days.push(d)
  return days
}

const leftDays  = computed(() => calendarDays(calendarMonth.value, calendarYear.value))
const rightDays = computed(() => calendarDays(nextMonth.value, nextYear.value))

function prevCal() {
  if (calendarMonth.value === 0) { calendarMonth.value = 11; calendarYear.value-- }
  else calendarMonth.value--
}
function nextCal() {
  if (calendarMonth.value === 11) { calendarMonth.value = 0; calendarYear.value++ }
  else calendarMonth.value++
}

function dateOf(day: number, month: number, year: number) {
  return new Date(year, month, day)
}

function isPast(day: number, month: number, year: number) {
  const d = dateOf(day, month, year)
  const today = new Date(); today.setHours(0,0,0,0)
  return d < today
}

function isSelected(day: number, month: number, year: number) {
  const d = dateOf(day, month, year)
  return (checkIn.value && sameDay(d, checkIn.value)) ||
         (checkOut.value && sameDay(d, checkOut.value))
}

function isInRange(day: number, month: number, year: number) {
  const d = dateOf(day, month, year)
  const end = hoverDate.value && selectingCheckout.value ? hoverDate.value : checkOut.value
  if (!checkIn.value || !end) return false
  return d > checkIn.value && d < end
}

function isRangeStart(day: number, month: number, year: number) {
  return checkIn.value ? sameDay(dateOf(day, month, year), checkIn.value) : false
}

function isRangeEnd(day: number, month: number, year: number) {
  const d = dateOf(day, month, year)
  const end = hoverDate.value && selectingCheckout.value ? hoverDate.value : checkOut.value
  return end ? sameDay(d, end) : false
}

function sameDay(a: Date, b: Date) {
  return a.getFullYear() === b.getFullYear() &&
         a.getMonth() === b.getMonth() &&
         a.getDate() === b.getDate()
}

function selectDay(day: number, month: number, year: number) {
  if (isPast(day, month, year)) return
  const d = dateOf(day, month, year)
  if (!selectingCheckout.value) {
    checkIn.value = d
    checkOut.value = null
    selectingCheckout.value = true
  } else {
    if (d <= checkIn.value!) {
      checkIn.value = d
      checkOut.value = null
    } else {
      checkOut.value = d
      selectingCheckout.value = false
      activeSection.value = 'who'
    }
  }
}

function clearDates() {
  checkIn.value = null
  checkOut.value = null
  selectingCheckout.value = false
}

const whenLabel = computed(() => {
  if (checkIn.value && checkOut.value) {
    return `${fmt(checkIn.value)} – ${fmt(checkOut.value)}`
  }
  if (checkIn.value) return `${fmt(checkIn.value)} – ?`
  return null
})

function fmt(d: Date) {
  return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
}

// ── WHO ───────────────────────────────────────────────────────────
const guests = ref(2)

// ── SEARCH ────────────────────────────────────────────────────────
const canSearch = computed(() =>
  location.value.trim().length > 0 && checkIn.value !== null && checkOut.value !== null
)

function handleSearch() {
  if (!canSearch.value) {
    if (!location.value.trim()) { activeSection.value = 'where'; return }
    if (!checkIn.value || !checkOut.value) { activeSection.value = 'when'; return }
  }
  const query: Record<string, string> = {}
  query.location = location.value
  query.checkIn  = checkIn.value!.toISOString().slice(0, 10)
  query.checkOut = checkOut.value!.toISOString().slice(0, 10)
  if (guests.value > 1) query.guests = String(guests.value)
  activeSection.value = null
  router.push({ name: 'search', query })
}

// ── CLICK OUTSIDE ─────────────────────────────────────────────────
const barRef = ref<HTMLElement | null>(null)

function onClickOutside(e: MouseEvent) {
  if (barRef.value && !barRef.value.contains(e.target as Node)) {
    activeSection.value = null
  }
}

onMounted(() => document.addEventListener('mousedown', onClickOutside))
onUnmounted(() => document.removeEventListener('mousedown', onClickOutside))

function openWhere() { activeSection.value = 'where' }
function openWhen()  { activeSection.value = 'when' }
function openWho()   { activeSection.value = 'who' }
</script>

<template>
  <section class="relative pt-16 pb-14 px-10 text-center">
    <!-- Subtle radial gradient blob behind heading -->
    <div class="pointer-events-none absolute inset-0 overflow-hidden flex items-start justify-center -top-20">
      <div class="w-[700px] h-[500px] rounded-full bg-[radial-gradient(ellipse_at_center,rgba(210,190,160,0.22)_0%,transparent_70%)]" />
    </div>

    <div class="relative max-w-2xl mx-auto">
      <h1 class="font-serif text-[clamp(44px,5.5vw,68px)] font-medium leading-[1.08] tracking-tight text-ink mb-4">
        Find your next <span class="font-script italic">escape</span>
      </h1>
      <p class="text-[15px] text-muted leading-relaxed mb-9 max-w-md mx-auto">
        Discover unique spaces to stay — from cozy mountain cabins to minimalist beachfront villas.
      </p>

      <!-- Search bar -->
      <div ref="barRef" class="relative">
        <form
          class="flex items-center bg-white rounded-full pl-6 pr-3 py-3 shadow-[0_2px_20px_rgba(0,0,0,0.10),0_0_0_1px_rgba(0,0,0,0.05)] transition-shadow"
          :class="{ 'shadow-[0_4px_28px_rgba(0,0,0,0.14),0_0_0_1px_rgba(0,0,0,0.06)]': activeSection }"
          @submit.prevent="handleSearch"
        >
          <!-- WHERE -->
          <button
            type="button"
            class="group flex-1 flex items-center gap-2.5 text-left px-2 py-1 rounded-full transition-colors min-w-0"
            :class="activeSection === 'where' ? 'bg-white' : 'hover:bg-gray-50'"
            @click="openWhere"
          >
            <MapPin class="w-4 h-4 text-muted shrink-0" :stroke-width="2" />
            <div class="flex flex-col min-w-0">
              <span class="text-[10px] font-semibold tracking-widest text-ink uppercase leading-none mb-1">WHERE</span>
              <span v-if="!location" class="text-sm text-dim truncate">Search destinations</span>
              <span v-else class="text-sm text-ink truncate font-medium">{{ location }}</span>
            </div>
          </button>

          <div class="w-px h-8 bg-divider shrink-0 mx-1" />

          <!-- WHEN -->
          <button
            type="button"
            class="group flex-1 flex items-center gap-2.5 text-left px-3 py-1 rounded-full transition-colors min-w-0"
            :class="activeSection === 'when' ? 'bg-white' : 'hover:bg-gray-50'"
            @click="openWhen"
          >
            <Calendar class="w-4 h-4 text-muted shrink-0" :stroke-width="2" />
            <div class="flex flex-col min-w-0">
              <span class="text-[10px] font-semibold tracking-widest text-ink uppercase leading-none mb-1">WHEN</span>
              <span v-if="!whenLabel" class="text-sm text-dim">Add dates</span>
              <span v-else class="text-sm text-ink font-medium truncate">{{ whenLabel }}</span>
            </div>
          </button>

          <div class="w-px h-8 bg-divider shrink-0 mx-1" />

          <!-- WHO -->
          <button
            type="button"
            class="group flex-[0.8] flex items-center gap-2.5 text-left px-3 py-1 rounded-full transition-colors min-w-0"
            :class="activeSection === 'who' ? 'bg-white' : 'hover:bg-gray-50'"
            @click="openWho"
          >
            <Users class="w-4 h-4 text-muted shrink-0" :stroke-width="2" />
            <div class="flex flex-col min-w-0">
              <span class="text-[10px] font-semibold tracking-widest text-ink uppercase leading-none mb-1">WHO</span>
              <span class="text-sm text-ink font-medium">{{ guests }} {{ guests === 1 ? 'guest' : 'guests' }}</span>
            </div>
          </button>

          <!-- Search button -->
          <button
            type="submit"
            class="w-12 h-12 rounded-full flex items-center justify-center shrink-0 ml-2 transition-all"
            :class="canSearch
              ? 'bg-dark text-white hover:bg-black hover:scale-105 active:scale-95 cursor-pointer'
              : 'bg-gray-200 text-gray-400 cursor-not-allowed'"
            aria-label="Search"
          >
            <Search class="w-5 h-5" :stroke-width="2.5" />
          </button>
        </form>

        <!-- WHERE popup -->
        <Transition name="popup">
          <div
            v-if="activeSection === 'where'"
            class="absolute top-[calc(100%+10px)] left-0 w-80 bg-white rounded-2xl shadow-[0_8px_40px_rgba(0,0,0,0.12),0_0_0_1px_rgba(0,0,0,0.05)] z-50 p-4"
          >
            <p class="text-[9px] font-bold tracking-[0.12em] text-ink/50 uppercase mb-2">WHERE</p>
            <input
              v-model="location"
              type="text"
              placeholder="Search destinations"
              class="w-full text-sm text-ink bg-gray-50 rounded-xl px-3 py-2.5 outline-none placeholder:text-dim focus:bg-white border border-transparent focus:border-divider transition-all"
              autofocus
              @keydown.enter.prevent="activeSection = 'when'"
            />
          </div>
        </Transition>

        <!-- WHEN calendar popup -->
        <Transition name="popup">
          <div
            v-if="activeSection === 'when'"
            class="absolute top-[calc(100%+10px)] left-1/2 -translate-x-1/2 bg-white rounded-2xl shadow-[0_8px_40px_rgba(0,0,0,0.12),0_0_0_1px_rgba(0,0,0,0.05)] z-50 p-5"
          >
            <div class="flex items-center justify-between mb-4 px-1">
              <p class="text-sm font-semibold text-ink">
                {{ selectingCheckout ? 'Select checkout date' : 'Select check-in date' }}
              </p>
              <button
                v-if="checkIn"
                type="button"
                class="text-xs text-muted hover:text-ink underline underline-offset-2 transition-colors"
                @click="clearDates"
              >Clear</button>
            </div>

            <div class="flex gap-6">
              <!-- Left month -->
              <div class="w-55">
                <div class="flex items-center justify-between mb-3">
                  <button type="button" class="p-1.5 rounded-full hover:bg-gray-100 transition-colors" @click="prevCal">
                    <ChevronLeft class="w-4 h-4 text-ink" :stroke-width="2" />
                  </button>
                  <span class="text-sm font-semibold text-ink">{{ MONTHS[calendarMonth] }} {{ calendarYear }}</span>
                  <div class="w-7" />
                </div>
                <div class="grid grid-cols-7 gap-y-1">
                  <div v-for="d in DAYS" :key="d" class="text-center text-[10px] font-semibold text-muted pb-1">{{ d }}</div>
                  <template v-for="(day, i) in leftDays" :key="i">
                    <div v-if="day === null" />
                    <button
                      v-else
                      type="button"
                      :disabled="isPast(day, calendarMonth, calendarYear)"
                      class="relative h-8 w-8 text-sm rounded-full flex items-center justify-center transition-all mx-auto"
                      :class="[
                        isPast(day, calendarMonth, calendarYear)
                          ? 'text-gray-300 cursor-not-allowed'
                          : 'cursor-pointer hover:bg-gray-100',
                        isSelected(day, calendarMonth, calendarYear)
                          ? 'bg-ink! text-white hover:bg-ink! font-semibold'
                          : '',
                        isInRange(day, calendarMonth, calendarYear)
                          ? 'bg-gray-100 rounded-none'
                          : '',
                        isRangeStart(day, calendarMonth, calendarYear)
                          ? 'rounded-l-full rounded-r-none' : '',
                        isRangeEnd(day, calendarMonth, calendarYear) && checkIn
                          ? 'rounded-r-full rounded-l-none' : '',
                      ]"
                      @click="selectDay(day, calendarMonth, calendarYear)"
                      @mouseenter="hoverDate = isPast(day, calendarMonth, calendarYear) ? null : dateOf(day, calendarMonth, calendarYear)"
                      @mouseleave="hoverDate = null"
                    >{{ day }}</button>
                  </template>
                </div>
              </div>

              <div class="w-px bg-divider-light" />

              <!-- Right month -->
              <div class="w-55">
                <div class="flex items-center justify-between mb-3">
                  <div class="w-7" />
                  <span class="text-sm font-semibold text-ink">{{ MONTHS[nextMonth] }} {{ nextYear }}</span>
                  <button type="button" class="p-1.5 rounded-full hover:bg-gray-100 transition-colors" @click="nextCal">
                    <ChevronRight class="w-4 h-4 text-ink" :stroke-width="2" />
                  </button>
                </div>
                <div class="grid grid-cols-7 gap-y-1">
                  <div v-for="d in DAYS" :key="d" class="text-center text-[10px] font-semibold text-muted pb-1">{{ d }}</div>
                  <template v-for="(day, i) in rightDays" :key="i">
                    <div v-if="day === null" />
                    <button
                      v-else
                      type="button"
                      :disabled="isPast(day, nextMonth, nextYear)"
                      class="relative h-8 w-8 text-sm rounded-full flex items-center justify-center transition-all mx-auto"
                      :class="[
                        isPast(day, nextMonth, nextYear)
                          ? 'text-gray-300 cursor-not-allowed'
                          : 'cursor-pointer hover:bg-gray-100',
                        isSelected(day, nextMonth, nextYear)
                          ? 'bg-ink! text-white hover:bg-ink! font-semibold'
                          : '',
                        isInRange(day, nextMonth, nextYear)
                          ? 'bg-gray-100 rounded-none'
                          : '',
                        isRangeStart(day, nextMonth, nextYear)
                          ? 'rounded-l-full rounded-r-none' : '',
                        isRangeEnd(day, nextMonth, nextYear) && checkIn
                          ? 'rounded-r-full rounded-l-none' : '',
                      ]"
                      @click="selectDay(day, nextMonth, nextYear)"
                      @mouseenter="hoverDate = isPast(day, nextMonth, nextYear) ? null : dateOf(day, nextMonth, nextYear)"
                      @mouseleave="hoverDate = null"
                    >{{ day }}</button>
                  </template>
                </div>
              </div>
            </div>
          </div>
        </Transition>

        <!-- WHO popup -->
        <Transition name="popup">
          <div
            v-if="activeSection === 'who'"
            class="absolute top-[calc(100%+10px)] right-0 w-72 bg-white rounded-2xl shadow-[0_8px_40px_rgba(0,0,0,0.12),0_0_0_1px_rgba(0,0,0,0.05)] z-50 p-5"
          >
            <div class="flex items-center justify-between">
              <div>
                <p class="text-sm font-semibold text-ink">Guests</p>
                <p class="text-xs text-muted mt-0.5">How many guests?</p>
              </div>
              <div class="flex items-center gap-3">
                <button
                  type="button"
                  class="w-8 h-8 rounded-full border border-divider flex items-center justify-center text-ink hover:border-ink transition-colors text-lg leading-none disabled:opacity-30 disabled:cursor-not-allowed"
                  :disabled="guests <= 1"
                  @click="guests = Math.max(1, guests - 1)"
                >−</button>
                <span class="text-sm font-semibold text-ink w-5 text-center">{{ guests }}</span>
                <button
                  type="button"
                  class="w-8 h-8 rounded-full border border-divider flex items-center justify-center text-ink hover:border-ink transition-colors text-lg leading-none disabled:opacity-30 disabled:cursor-not-allowed"
                  :disabled="guests >= 20"
                  @click="guests = Math.min(20, guests + 1)"
                >+</button>
              </div>
            </div>
          </div>
        </Transition>
      </div>
    </div>
  </section>
</template>

<style scoped>
.popup-enter-active,
.popup-leave-active {
  transition: opacity 0.15s ease, transform 0.15s ease;
}
.popup-enter-from,
.popup-leave-to {
  opacity: 0;
  transform: translateY(6px);
}
</style>
