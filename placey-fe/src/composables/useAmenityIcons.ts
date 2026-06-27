import {
  Wifi,
  Car,
  UtensilsCrossed,
  Wind,
  Waves,
  Tv,
  Dumbbell,
  Dog,
  Flame,
  Coffee,
  Snowflake,
  WashingMachine,
  Lock,
  Camera,
  Sparkles,
} from '@lucide/vue'

const amenityIcons: Record<string, object> = {
  WiFi: Wifi,
  Parking: Car,
  Kitchen: UtensilsCrossed,
  'Air conditioning': Wind,
  Pool: Waves,
  TV: Tv,
  Gym: Dumbbell,
  'Pets allowed': Dog,
  Fireplace: Flame,
  'Coffee maker': Coffee,
  Heating: Snowflake,
  Washer: WashingMachine,
  Dryer: WashingMachine,
  Safe: Lock,
  'Security cameras': Camera,
}

export function useAmenityIcons() {
  function amenityIcon(name: string): object {
    return amenityIcons[name] ?? Sparkles
  }

  return { amenityIcon }
}
