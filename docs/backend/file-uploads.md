# Backend - File Uploads

---

## Upload endpoints

| Method | Endpoint                        | Auth  | Field  | Description                                  |
| ------ | ------------------------------- | :---: | ------ | -------------------------------------------- |
| POST   | `/listings/:id/photos`          |   ✅   | `file` | Upload a photo for a listing (host only)     |
| DELETE | `/listings/:id/photos/:photoId` |   ✅   | —      | Delete a listing photo (host only)           |
| GET    | `/listings/:id/photos`          |   ✅   | —      | Get ordered photos for a listing (host only) |
| POST   | `/users/me/avatar`              |   ✅   | `file` | Upload a profile avatar                      |

Both upload endpoints accept `multipart/form-data` with the file in a field named `file`.

**Validation rules (enforced by the service layer):**

| Endpoint                    | Max size | Allowed types                           |
| --------------------------- | -------- | --------------------------------------- |
| `POST /users/me/avatar`     | 5 MB     | `image/jpeg`, `image/png`, `image/webp` |
| `POST /listings/:id/photos` | 10 MB    | `image/jpeg`, `image/png`, `image/webp` |

Violations return `400 Bad Request` with a `detail` message via `ErrorHandlingMiddleware`. The frontend also validates these constraints before uploading so the user sees an immediate error without a round-trip.

---

## Storage

Files are saved to the local filesystem under the `wwwroot/` directory in the backend container. The `UseStaticFiles()` middleware in `Program.cs` serves these files directly over HTTP.

The relative URL stored in the database (e.g. `/uploads/photos/abc123.jpg`) is returned in API responses as `url` / `photoUrls` / `avatarUrl`. The frontend prepends the API base URL to construct the full image URL.

---

## File naming

Uploaded files are saved with a GUID-based filename to avoid collisions and prevent path traversal:

```
wwwroot/uploads/photos/<guid><extension>   # listing photos
wwwroot/uploads/avatars/<guid><extension>   # user avatars
```

The original filename from the upload is not used.

---

## Docker volume

The `wwwroot/` directory is mounted as a named Docker volume to persist uploaded files across container restarts:

```yaml
# docker-compose.yml
volumes:
  - uploads:/app/wwwroot

volumes:
  uploads:
```

---

## Photo ordering

`ListingPhoto` records have an `Order` field (integer). `ListingService.GetPhotosAsync` returns photos sorted by `Order` ascending. When photos are uploaded, `Order` is set to `current max + 1` to append to the end of the sequence.

---

## Photo deletion ordering

The deletion flow commits the database record first, then attempts to remove the physical file:

1. `db.ListingPhotos.Remove(photo)` + `SaveChangesAsync()` — atomically removes the DB record.
2. `File.Delete(physicalPath)` — best-effort; failures are logged as warnings but do not affect the response. A failed file delete leaves an orphaned file on disk, which is preferable to a failed DB delete that leaves a dangling record.

## Limitations & future considerations

- **Local storage only**: for production at scale, consider migrating to cloud object storage (AWS S3, Azure Blob Storage, Cloudflare R2) to support horizontal scaling and CDN delivery.
- **No image resizing**: photos are stored at their original size; consider adding server-side resize/compress to reduce storage and load times.
