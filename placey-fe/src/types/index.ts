export type ListingCategory =
  | 'Beach'
  | 'Mountains'
  | 'Urban'
  | 'Luxe'
  | 'Countryside'
  | 'Island'
  | 'Lake'
export type PropertyType = 'Apartment' | 'House'

export interface Listing {
  id: string
  title: string
  description: string
  city: string
  country: string
  location: string
  pricePerNight: number
  bedrooms: number
  bathrooms: number
  maxGuests: number
  isInstantBook: boolean
  amenities: string[]
  category: ListingCategory
  propertyType: PropertyType
  photoUrls: string[]
  averageRating: number | null
  reviewCount: number
  hostId: string
  hostName: string
  createdAt: string
}

export interface ListingsResponse {
  items: Listing[]
  total: number
}

export interface ListingSearchParams {
  location?: string
  checkIn?: string
  checkOut?: string
  guests?: number
  category?: ListingCategory
  propertyType?: PropertyType
  minBedrooms?: number
  minPrice?: number
  maxPrice?: number
  page?: number
  limit?: number
}

export interface Category {
  id: string
  label: string
  icon: object
}

export interface AvailabilityRange {
  id: string
  startDate: string
  endDate: string
  isBlocked: boolean
}

export interface Review {
  id: string
  listingId: string
  authorId: string
  authorName: string
  authorAvatarUrl: string | null
  rating: number
  comment: string
  createdAt: string
}
