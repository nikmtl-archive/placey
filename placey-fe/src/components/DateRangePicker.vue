<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted } from 'vue'
import { Calendar, ChevronLeft, ChevronRight } from '@lucide/vue'

const props = defineProps<{
  checkIn:   string
  checkOut:  string
  // When true the component renders no trigger button — the parent opens/closes via expose
  noTrigger?: boolean
}>()

const emit = defineEmits<{
  'update:checkIn':  [value: string]
  'update:checkOut': [value: string]
}>()

// Parse / fmt 
function parseDate(s?: string): Date | null {
  if (!s) return null
  const d = new Date(s + 'T00:00:00')
  return isNaN(d.getTime()) ? null : d
}
function fmt(d: Date) {
  return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
}
function toISO(d: Date) {
  return d.toISOString().slice(0, 10)
}

// Internal date state
const internalIn  = ref<Date | null>(parseDate(props.checkIn))
const internalOut = ref<Date | null>(parseDate(props.checkOut))

watch(() => props.checkIn,  v => { internalIn.value  = parseDate(v) })
watch(() => props.checkOut, v => { internalOut.value = parseDate(v) })

// Popup state 
const open              = ref(false)
const selectingCheckout = ref(false)
const hoverDate         = ref<Date | null>(null)

const calendarMonth = ref(internalIn.value?.getMonth()    ?? new Date().getMonth())
const calendarYear  = ref(internalIn.value?.getFullYear() ?? new Date().getFullYear())

const MONTHS = ['January','February','March','April','May','June',
                'July','August','September','October','November','December']
const DAYS   = ['Su','Mo','Tu','We','Th','Fr','Sa']

const nextMonth = computed(() => calendarMonth.value === 11 ? 0                     : calendarMonth.value + 1)
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

function dateOf(day: number, month: number, year: number) { return new Date(year, month, day) }
function isPast(day: number, month: number, year: number) {
  const today = new Date(); today.setHours(0, 0, 0, 0)
  return dateOf(day, month, year) < today
}
function sameDay(a: Date, b: Date) {
  return a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth() && a.getDate() === b.getDate()
}
function isSelected(day: number, month: number, year: number) {
  const d = dateOf(day, month, year)
  return (internalIn.value && sameDay(d, internalIn.value)) || (internalOut.value && sameDay(d, internalOut.value))
}
function isInRange(day: number, month: number, year: number) {
  const d   = dateOf(day, month, year)
  const end = hoverDate.value && selectingCheckout.value ? hoverDate.value : internalOut.value
  if (!internalIn.value || !end) return false
  return d > internalIn.value && d < end
}
function isRangeStart(day: number, month: number, year: number) {
  return internalIn.value ? sameDay(dateOf(day, month, year), internalIn.value) : false
}
function isRangeEnd(day: number, month: number, year: number) {
  const d   = dateOf(day, month, year)
  const end = hoverDate.value && selectingCheckout.value ? hoverDate.value : internalOut.value
  return end ? sameDay(d, end) : false
}

function selectDay(day: number, month: number, year: number) {
  if (isPast(day, month, year)) return
  const d = dateOf(day, month, year)
  if (!selectingCheckout.value) {
    internalIn.value = d; internalOut.value = null; selectingCheckout.value = true
    emit('update:checkIn', toISO(d)); emit('update:checkOut', '')
  } else {
    if (d <= internalIn.value!) {
      internalIn.value = d; internalOut.value = null
      emit('update:checkIn', toISO(d)); emit('update:checkOut', '')
    } else {
      internalOut.value = d; selectingCheckout.value = false; open.value = false
      emit('update:checkOut', toISO(d))
    }
  }
}

function clearDates() {
  internalIn.value = null; internalOut.value = null; selectingCheckout.value = false
  emit('update:checkIn', ''); emit('update:checkOut', '')
}

function openPicker() {
  selectingCheckout.value = !internalIn.value || !!internalOut.value ? false : true
  open.value = true
}
function closePicker() { open.value = false }

defineExpose({ openPicker, closePicker })

// Label
const label = computed(() => {
  if (internalIn.value && internalOut.value) return `${fmt(internalIn.value)} – ${fmt(internalOut.value)}`
  if (internalIn.value) return `${fmt(internalIn.value)} – ?`
  return null
})

// Click outside
const rootRef = ref<HTMLElement | null>(null)
function onClickOutside(e: MouseEvent) {
  if (rootRef.value && !rootRef.value.contains(e.target as Node)) open.value = false
}
onMounted(() => document.addEventListener('mousedown', onClickOutside))
onUnmounted(() => document.removeEventListener('mousedown', onClickOutside))
</script>

<template>
  <div ref="rootRef" class="relative">
    <!-- Self-contained trigger (used in ListingView booking panel) -->
    <button
      v-if="!noTrigger"
      type="button"
      class="w-full flex items-center gap-2 px-3 py-2.5 rounded-xl border border-divider hover:border-ink/40 transition-colors text-sm bg-white text-left cursor-pointer"
      :class="open ? 'border-ink/40 ring-2 ring-ink/10' : ''"
      @click="openPicker"
    >
      <Calendar :size="15" class="text-muted shrink-0" />
      <span v-if="label" class="text-ink font-medium truncate">{{ label }}</span>
      <span v-else class="text-dim">Add dates</span>
    </button>

    <!-- Calendar popup -->
    <Transition name="picker-popup">
      <div
        v-if="open"
        class="absolute top-[calc(100%+8px)] left-1/2 -translate-x-1/2 bg-white rounded-2xl shadow-[0_8px_40px_rgba(0,0,0,0.14),0_0_0_1px_rgba(0,0,0,0.06)] z-50 p-5"
      >
        <div class="flex items-center justify-between mb-4 px-1">
          <p class="text-sm font-semibold text-ink">
            {{ selectingCheckout ? 'Select checkout date' : 'Select check-in date' }}
          </p>
          <button
            v-if="internalIn"
            type="button"
            class="text-xs text-muted hover:text-ink underline underline-offset-2 transition-colors cursor-pointer"
            @click="clearDates"
          >Clear</button>
        </div>

        <div class="flex gap-6">
          <!-- Left month -->
          <div class="w-55">
            <div class="flex items-center justify-between mb-3">
              <button type="button" class="p-1.5 rounded-full hover:bg-gray-100 transition-colors cursor-pointer" @click="prevCal">
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
                  v-else type="button" :disabled="isPast(day, calendarMonth, calendarYear)"
                  class="relative h-8 w-8 text-sm rounded-full flex items-center justify-center transition-all mx-auto"
                  :class="[
                    isPast(day, calendarMonth, calendarYear) ? 'text-gray-300 cursor-not-allowed' : 'cursor-pointer hover:bg-gray-100',
                    isSelected(day, calendarMonth, calendarYear) ? 'bg-ink! text-white hover:bg-ink! font-semibold' : '',
                    isInRange(day, calendarMonth, calendarYear) ? 'bg-gray-100 rounded-none' : '',
                    isRangeStart(day, calendarMonth, calendarYear) ? 'rounded-l-full rounded-r-none' : '',
                    isRangeEnd(day, calendarMonth, calendarYear) && internalIn ? 'rounded-r-full rounded-l-none' : '',
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
              <button type="button" class="p-1.5 rounded-full hover:bg-gray-100 transition-colors cursor-pointer" @click="nextCal">
                <ChevronRight class="w-4 h-4 text-ink" :stroke-width="2" />
              </button>
            </div>
            <div class="grid grid-cols-7 gap-y-1">
              <div v-for="d in DAYS" :key="d" class="text-center text-[10px] font-semibold text-muted pb-1">{{ d }}</div>
              <template v-for="(day, i) in rightDays" :key="i">
                <div v-if="day === null" />
                <button
                  v-else type="button" :disabled="isPast(day, nextMonth, nextYear)"
                  class="relative h-8 w-8 text-sm rounded-full flex items-center justify-center transition-all mx-auto"
                  :class="[
                    isPast(day, nextMonth, nextYear) ? 'text-gray-300 cursor-not-allowed' : 'cursor-pointer hover:bg-gray-100',
                    isSelected(day, nextMonth, nextYear) ? 'bg-ink! text-white hover:bg-ink! font-semibold' : '',
                    isInRange(day, nextMonth, nextYear) ? 'bg-gray-100 rounded-none' : '',
                    isRangeStart(day, nextMonth, nextYear) ? 'rounded-l-full rounded-r-none' : '',
                    isRangeEnd(day, nextMonth, nextYear) && internalIn ? 'rounded-r-full rounded-l-none' : '',
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
  </div>
</template>

<style scoped>
.picker-popup-enter-active, .picker-popup-leave-active { transition: opacity 0.15s ease, transform 0.15s ease; }
.picker-popup-enter-from, .picker-popup-leave-to { opacity: 0; transform: translateY(6px); }
</style>
