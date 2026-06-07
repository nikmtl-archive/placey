import { onMounted, onUnmounted } from 'vue'
import type { Ref } from 'vue'

export function useClickOutside(target: Ref<HTMLElement | null>, handler: () => void) {
  function onMousedown(event: MouseEvent) {
    if (target.value && !target.value.contains(event.target as Node)) {
      handler()
    }
  }

  onMounted(() => document.addEventListener('mousedown', onMousedown))
  onUnmounted(() => document.removeEventListener('mousedown', onMousedown))
}
