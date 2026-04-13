# DnD backend hasznalatlan elemek audit

Datum: 2026-03-23
Projekt: DnD_UT-Backend (GameApi) vs DnD_UT-Frontend (dnd-frontend/src)
Kiegeszites: ellenorizve a DnD-Desktop (MyWpfApp) klienst is

## Modszertan
- Backend endpoint lista: GameApi Controller atributumokbol (Route + HttpGet/Post/Put/Delete/Patch).
- Frontend hasznalat: axios/api/apiUpload hivasokbol, HTTP metodus + path alapon.
- Osszevetes: endpoint akkor szamit hasznaltnak, ha ugyanaz a HTTP metodus es azonos statikus route-szegmensek megtalalhatok frontend hivasban.
- Ez statikus kodalapu audit, runtime traffic/log nincs figyelembe veve.

## Teljesen nem hasznalt controller (csak WEB frontend nezopontjabol)
- AdminUsersController
  - GET /api/admin/users
  - GET /api/admin/users/{id}
  - PUT /api/admin/users/{id}/role
  - PUT /api/admin/users/{id}/status

Megjegyzes: a DnD-Desktop viszont hasznalja ezeket, igy globalisan NEM tekinthetoek hasznalatlannak.

## DnD-Desktop altal hasznalt admin endpointok
- GET /api/auth/me
- GET /api/admin/users
- PUT /api/admin/users/{id}/role
- PUT /api/admin/users/{id}/status

Forras: DnD-Desktop/MyWpfApp/Services/ApiClient.cs

## Nem hasznalt endpointok frontend szerint

### AdminUsersController
- GET /api/admin/users
- GET /api/admin/users/{id}
- PUT /api/admin/users/{id}/role
- PUT /api/admin/users/{id}/status

### BooksController
- GET /api/Books
- GET /api/Books/{id:int}
- POST /api/Books
- DELETE /api/Books/{id:int}

Books oldal adatforrasa NEM ez a 4 endpoint, hanem:
- GET /api/Books/markdown
- GET /api/Books/markdown/{fileName}

Ezek a backend Books mappabol (*.md fajlok) olvasnak, nem a Books adatbazis tablabol.

### ChatController
- POST /api/Chat/create-room
- POST /api/Chat/invite
- POST /api/Chat/join
- POST /api/Chat/leave

### FriendController
- GET /api/Friend/blocked
- GET /api/Friend/notifications
- GET /api/Friend/online

### MapForgeController
- DELETE /api/mapforge/campaigns/{id}
- PATCH /api/mapforge/campaigns/{id}/name
- POST /api/mapforge/campaigns/{id}/nodes
- PUT /api/mapforge/campaigns/{id}/nodes/{nodeId}
- DELETE /api/mapforge/campaigns/{id}/nodes/{nodeId}
- POST /api/mapforge/campaigns/{id}/edges
- DELETE /api/mapforge/campaigns/{id}/edges/{edgeId}
- POST /api/mapforge/campaigns/{id}/invites/friend
- POST /api/mapforge/campaigns/{id}/invites/link
- GET /api/mapforge/invites
- POST /api/mapforge/invites/{inviteId}/accept
- POST /api/mapforge/invites/{inviteId}/decline
- POST /api/mapforge/invites/claim

Osszesen nem hasznalt endpoint: 28
Osszes backend endpoint: 165

## Egyeb backend elemek, amik jelenleg varhatoan nincsenek hasznalva

### Service
- GameApi/Services/JWTService.cs
  - Regisztralva van Program.cs-ben (AddScoped), de nincs ra tenyleges hivatkozas/injektalas a controllerekben.

### DTO-k (admin)
- GameApi/DTOs/Admin/AdminUserDto.cs
- GameApi/DTOs/Admin/UpdateUserRoleDto.cs
- GameApi/DTOs/Admin/UpdateUserStatusDto.cs
  - Ezek csak az AdminUsersController-ben vannak hasznalva, ami frontend szerint jelenleg teljesen hasznalatlan.

## Megjegyzes
- Ha vannak kulso kliensek (nem a DnD_UT-Frontend), azok hasznalhatnak itt hasznalatlannak jelolt endpointokat.
- DnD-Desktop konkretan hasznalja az admin endpointokat, ezeket nem szabad torolni.
- Ha szeretned, kovetkezo korben adok egy biztonsagos torlesi/prioritasi javaslatot (mi torolheto azonnal, mi menjen deprecate allapotba, mihez kell feature-flag).
