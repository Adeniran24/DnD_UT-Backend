# DnD UT Backend - Szakmai Dokumentacio

## 1. Attekintes

Ez a dokumentum a backend rendszer teljes API-feluletenek mukodeset, tervezesi donteseit es az implementacio szakmai indoklasat irja le.

A rendszer technologiai alapja:
- ASP.NET Core Web API
- Entity Framework Core + MySQL/MariaDB
- JWT alapu autentikacio es autorizacio
- SignalR valos ideju kommunikaciohoz
- JSON-fajl alapanyagra epulo wiki modulok

A backend ket nagy retegben mukodik:
- Tranzakcionalis/app adat: relacios adatbazis (felhasznalok, karakterek, baratkapcsolatok, VTT, MapForge, stb.)
- Referencia DnD tartalom: statikus JSON allomanyok betoltese es query-zese (2014 SRD wiki vegpontok)

## 2. Globalis architektura es futasi pipeline

### 2.1 Konfiguracio es host setup

A konfiguracio explicit betoltesre kerul:
- appsettings.json
- appsettings.{Environment}.json
- environment valtozok

Ez a minta biztosabb deployment viselkedest ad konteneres/reverse proxy korzetben, mert nem implicit startup logikara hagyatkozik.

### 2.2 Infrastruktura

- Swagger konfiguralt Bearer security definicioval
- CORS policy rugalmasan kezeli a production domaint es a local frontendet
- JWT auth validalja: issuer, audience, alairas, lejarat
- SignalR hubokhoz JWT query param (`access_token`) tamogatott
- Forwarded headers tamogatott reverse proxy mogotti futtatasra
- Statikus fajl kiszolgalas tobb rootbol

### 2.3 Miért ilyen a middleware sorrend

A sorrend szakmailag helyes egy auth-olt API + static + hub hibrid apphoz:
1. proxy header normalizalas
2. static files
3. routing
4. CORS
5. authentication
6. authorization
7. controllers + hubs mapping

Ennek eredmenye:
- auth informacio mar elerheto a controllerekben/hubokban
- CORS preflightok megfeleloen mukodnek
- websocket upgrade auth mellett is stabil

## 3. Biztonsagi modell

### 3.1 AuthN/AuthZ

- JWT alapu azonositas
- Kontroller szintu es endpoint szintu `[Authorize]`
- Admin vegpontoknal role-alapu vedes: `Authorize(Roles = "Admin")`
- A nem publikus vegpontok altalaban claim-bol olvassak a felhasznalo azonositojat

### 3.2 Miért claim-fallback mintat hasznal

Tobb helyen NameIdentifier/id/userId/sub claim fallback van. Ennek oka, hogy kulonbozo tokenkiallitasokkal (kulon service, kulon claim mapping) is kompatibilis maradjon a rendszer.

### 3.3 Fajlfeltoltes vedelmek

- MIME tipus white-list
- Max meret limit (tipikusan 5MB vagy 10MB)
- Random, szerver altal kepzett file-nev
- Nem trusted user file-nev hasznalata

Ez csokkenti a feltoltesi tamadasi feluletet (path traversal, extension spoofing, oversized payload).

## 4. Domain szerinti endpoint dokumentacio

## 4.1 Auth API (`/api/auth`)

### Endpointok

1. `POST /api/auth/register`
- Query: `email`, `username`, `password`
- Funkcio: uj user letrehozasa
- Mukodes: duplikalt email ellenorzes, kliens oldali hash ujra-hashelve (SHA256), default role `User`
- Valasz: `200 OK` vagy `400`

2. `POST /api/auth/login`
- Query: `email`, `password`
- Funkcio: bejelentkezes + JWT kiadas
- Mukodes: aktiv statusz ellenorzes, hash compare, `LastLoginAt` frissites
- Valasz: `200 { token }`, vagy `401`, `400`

3. `GET /api/auth/me` (Authorize)
- Funkcio: aktualis user profiladat
- Valasz: alapprofil + tema + tutorial flag

4. `GET /api/auth/me/theme` (Authorize)
- Funkcio: profil tema JSON visszaadas
- Mukodes: JSON parse vedetten, hibas serializacio esetben null tema

5. `PUT /api/auth/me/theme` (Authorize)
- Body: `{ theme: JsonElement }`
- Funkcio: profil tema mentes

6. `PUT /api/auth/me/tutorial` (Authorize)
- Body: `{ completed: bool }`
- Funkcio: onboarding statusz frissites

7. `PUT /api/auth/me` (Authorize)
- Body: `username/email/currentPassword/newPassword`
- Funkcio: profil update
- Fontos szabaly: email/jelszo valtoztatas csak current password ellenorzessel

8. `PUT /api/auth/me/profile-picture` (Authorize, JSON consume)
- Body: `{ profilePicture: string }`
- Funkcio: preset vagy mar feltoltott kep URL beallitasa
- White-listelt prefix: `/defaults/` vagy `/uploads/`

9. `PUT /api/auth/me/profile-picture` (Authorize, multipart/form-data)
- Body: `file`
- Funkcio: uj profilkep feltoltes
- Mellekhatas: regi feltoltott profilkep torlese, ha helyi uploads alol jott

10. `GET /api/auth/salt`
- Query: `email`
- Funkcio: user salt kiadasa

11. `POST /api/auth/salt-send`
- Body: `{ email, salt }`
- Funkcio: salt tarolasa regisztracio utan

### Miért igy valosult meg

- A bejelentkezesi folyamatban kulon salt endpointokkal lehet kliens oldali hash strategiat hasznalni.
- Profil update-ben kulon biztonsagi kontroll van az erzekeny mezokre (email, jelszo), ez jo gyakorlat.
- Kettos profile picture endpoint ugyanazon route-on tartalomtipus alapjan valaszt, ami API ergonomia szempontbol tiszta kliensoldali szerzodest ad.

## 4.2 Admin API (`/api/admin/users`)

1. `GET /api/admin/users`
- Osszes user listazasa admin DTO-val

2. `GET /api/admin/users/{id}`
- Egy user lekerese

3. `PUT /api/admin/users/{id}/role`
- Role update (`User`, `DM`, `Admin`)
- Vedelmi szabaly: admin nem veheti le a sajat admin role-jet

4. `PUT /api/admin/users/{id}/status`
- Aktiv/bannolt statusz
- Vedelmi szabaly: admin nem deaktivallhatja sajat fiokjat

### Miért igy valosult meg

Az admin API explicit korlatokkal vedi a rendszer-admin allapot konzisztenciajat, megelozve az on-locked out szcenariokat.

## 4.3 Characters API (`/api/characters`)

1. `GET /api/characters`
- Sajat karakterek listaja

2. `GET /api/characters/{id}`
- Sajat karakter egyedi lekeres

3. `POST /api/characters`
- Uj karakter letrehozas
- Kiemelt logika: JSON mezo normalizalas (`equipment`, `attacks`, `spellbook`, `featuresFeats`)

4. `PUT /api/characters/{id}`
- Sajat karakter update
- Kiemelt logika: immutable mezok vedelme (`id`, `userId`, `created_at`)

5. `DELETE /api/characters/{id}`
- Sajat karakter torles

### Miért igy valosult meg

A normalizalo strategia csokkenti a hibas vagy torott JSON payload hatasat es biztos allapotot tart fenn a frontend szerkesztesi workflowhoz.

## 4.4 Friend API (`/api/friend`)

1. `GET /api/friend/list`
2. `GET /api/friend/search?query=`
3. `GET /api/friend/status/{targetUserId}`
4. `POST /api/friend/add?username=`
5. `POST /api/friend/respond?requestId=&action=accept|decline`
6. `GET /api/friend/requests`
7. `DELETE /api/friend/{friendId}`
8. `POST /api/friend/block?userIdToBlock=`
9. `GET /api/friend/blocked`
10. `POST /api/friend/unblock?userIdToUnblock=`
11. `GET /api/friend/mutual/{otherUserId}`
12. `GET /api/friend/online` (placeholder)
13. `GET /api/friend/notifications` (placeholder)
14. `POST /api/friend/invite-multiple?userIds=1,2,3` (utility)

### Miért igy valosult meg

- A friendship entitas statusz-gepkent mukodik (Pending/Accepted/Declined/Blocked), ami egyszerusiti a kapcsolati logikat.
- A block kepes kapcsolat nelkul is uj rekordot letrehozni, ez praktikus moderation UX-et ad.
- Placeholder endpointok jelzik az evolucios utat real-time jelenlet/ertesites iranyaba.

## 4.5 Chat API (`/api/chat`)

1. `POST /api/chat/create-room?roomName=`
2. `POST /api/chat/invite?roomId=&username=`
3. `POST /api/chat/join?roomId=`
4. `POST /api/chat/send?roomId=...` vagy `recipientUsername=...`
5. `GET /api/chat/messages?channelId=`
6. `POST /api/chat/leave?roomId=`

### Miért igy valosult meg

- A room membership ellenorzes minden kritikus ponton megtortenik.
- A privat uzenet API jelzi, hogy a vegpont interface mar elokeszitett, de a vegso adatszerkezeti tamogatas kulon DM rendszerbe kerult.

## 4.6 Direct Message API (`/api/dm`)

1. `GET /api/dm/with/{friendId}`
- Csak elfogadott baratkapcsolat mellett ad vissza historikat
- Eredmeny sender/receiver metadata-val

### Miért igy valosult meg

A baratsag alapfeltetel hard authorization check a query eloott, ezzel adatszivargas es felhasznaloi snooping elkerulheto.

## 4.7 VTT REST API (`/api/vtt`)

1. `GET /api/vtt/sessions`
2. `POST /api/vtt/sessions`
3. `POST /api/vtt/sessions/{id}/join`
4. `GET /api/vtt/sessions/{id}/state`
5. `PUT /api/vtt/sessions/{id}/map`
6. `POST /api/vtt/sessions/{id}/map/image` (multipart)
7. `POST /api/vtt/sessions/{id}/assets` (multipart)

### Fobb viselkedes

- Session role modell: DM vagy Player
- `state` endpoint aggregalt snapshotot ad: map + tokenek + members + utolso 50 chat + initiative
- Nem DM user rejtett tokeneket csak sajat tulajdon eseten lat
- Map es asset modositasok SignalR broadcasttal szinkronizalnak

### Miért igy valosult meg

A REST snapshot + SignalR delta minta klasszikus, skalahato VTT architektura:
- REST: reconnect/refresh utan teljes allapot
- SignalR: valos ideju inkrementalis update

## 4.8 VTT SignalR Hub (`/hubs/vtt`)

### Kapcsolodas
- JWT `access_token` query tamogatott
- Group kulcs: `vtt:{sessionId}`

### Hub methodok
1. `JoinSession(sessionId)`
2. `LeaveSession(sessionId)`
3. `SendChat(sessionId, content)`
4. `RollDice(sessionId, expression)`
5. `SendPing(sessionId, {x,y})`
6. `CreateToken(sessionId, request)`
7. `UpdateToken(sessionId, request)`
8. `DeleteToken(sessionId, tokenId)`
9. `UpdateMap(sessionId, request)`
10. `AddInitiativeEntry(sessionId, request)`
11. `UpdateInitiativeEntry(sessionId, request)`
12. `RemoveInitiativeEntry(sessionId, entryId)`
13. `ClearInitiative(sessionId)`
14. `ResetInitiative(sessionId)`
15. `StepInitiative(sessionId, direction)`
16. `SetInitiativeActive(sessionId, entryId)`

### Broadcast eventek
- `chatReceived`
- `ping`
- `tokenCreated`
- `tokenUpdated`
- `tokenDeleted`
- `mapUpdated`
- `initiativeUpdated`

### Miért igy valosult meg

A DM-only mutaciok (map, initiative, extra token jogok) asztali VTT workflowt tukrozik, mig a player oldali iranyitott token update ownership kontrollal vedett.

## 4.9 Direct Message SignalR Hub (`/hubs/dm`)

### Hub method
1. `SendDm(friendUserId, content)`

### Mukodes
- Uzenet kuldes elott friendship accepted ellenorzes
- Persistalas adatbazisba
- Kuldes mindket fel user channelere (`Clients.Users(me, friend)`)

### Miért igy valosult meg

A perszisztencia elotti jogosultsagellenorzes + ketoldalu push biztositja a konzisztens inbox/allapot frissitest.

## 4.10 MapForge API (`/api/mapforge`)

### Kampanyok
1. `GET /api/mapforge/campaigns`
2. `GET /api/mapforge/campaigns/{id}`
3. `POST /api/mapforge/campaigns`
4. `PUT /api/mapforge/campaigns/{id}`
5. `PATCH /api/mapforge/campaigns/{id}/name`
6. `DELETE /api/mapforge/campaigns/{id}`
7. `POST /api/mapforge/campaigns/{id}/cover` (multipart)

### Node-ok
8. `POST /api/mapforge/campaigns/{id}/nodes`
9. `PUT /api/mapforge/campaigns/{id}/nodes/{nodeId}`
10. `DELETE /api/mapforge/campaigns/{id}/nodes/{nodeId}`
11. `POST /api/mapforge/campaigns/{id}/nodes/{nodeId}/image` (multipart)
12. `DELETE /api/mapforge/campaigns/{id}/nodes/{nodeId}/image`

### Edge-ek
13. `POST /api/mapforge/campaigns/{id}/edges`
14. `DELETE /api/mapforge/campaigns/{id}/edges/{edgeId}`

### Share-ek
15. `GET /api/mapforge/campaigns/{id}/shares`
16. `POST /api/mapforge/campaigns/{id}/shares` (deprecated flow, hiba uzenetet ad)
17. `PUT /api/mapforge/campaigns/{id}/shares/{shareUserId}`
18. `DELETE /api/mapforge/campaigns/{id}/shares/{shareUserId}`

### Invite rendszer
19. `POST /api/mapforge/campaigns/{id}/invites/friend`
20. `POST /api/mapforge/campaigns/{id}/invites/link`
21. `GET /api/mapforge/invites`
22. `POST /api/mapforge/invites/{inviteId}/accept`
23. `POST /api/mapforge/invites/{inviteId}/decline`
24. `POST /api/mapforge/invites/claim`

### Fobb jogosultsagi es uzleti szabalyok

- Access role: `owner`, `editor`, `viewer`
- `editor` korlatozas: nem torolhet node-ot (save diff alapjan ellenorizve)
- `owner` torolhet kampanyt es menedzselhet share/invite flowt
- Link invite tokenes, rovid elettartamu
- Friend invite csak elfogadott baratoknak

### Miért igy valosult meg

- A role modell lehetove teszi a kollaboracios szerkesztest anelkul, hogy tul sok adatkiserletet engednenk.
- A JSON-ben tarolt node/edge graf gyors iteraciot ad frontend graf editorhoz migracios overhead nelkul.
- Az invite rendszer atomikus allapotatmenetekkel kezeli a pending/accepted/declined/expired eletciklust.

## 4.11 Books API (`/api/books`)

1. `GET /api/books`
2. `GET /api/books/{id}`
3. `GET /api/books/markdown`
4. `GET /api/books/markdown/{fileName}`
5. `POST /api/books` (multipart)
6. `DELETE /api/books/{id}`

### Kiemelt logika

- `markdown` endpoint a szerver oldali Books mappat indexeli
- cover kep eleresi utvonal markdown sorokbol regex-szel detektalhato
- markdown file olvasasnal path traversal ellen `Path.GetFileName` + `.md` enforce

### Miért igy valosult meg

A markdown alapanyag kiszolgalasa kulon adatbazis feltoltes nelkul teszi lehetove a content-vezerelet olvasast, gyors editorial workflowval.

## 4.12 PDF API (`/api/pdf`)

1. `POST /api/pdf/upload`
2. `GET /api/pdf/template`
3. `POST /api/pdf/save/{id}`
4. `GET /api/pdf/final/{id}`

### Kiemelt logika

- Template PDF szerver oldali sablon
- Mezoadatok JSON-kent tarolva DB-ben
- Final endpoint iText-tel ujrageneral editable PDF-et ad vissza
- Mezok flatten nelkul maradnak, hogy webes szerkesztoben tovabb modositheto legyen

### Miért igy valosult meg

A sablon + adat kulon tarolasa fenntarthato, mivel a statikus PDF kulon verziozhato, az user input pedig strukturaltan update-elheto.

## 4.13 Wiki/Reference API-k (DnD 2014)

Ezek a vegpontok jellemzoen statikus JSON allomanybol olvasnak.

### AbilityScores (`/api/2014/ability-scores`)
1. `GET /api/2014/ability-scores`
2. `GET /api/2014/ability-scores/{index}`

### Alignments (`/api/2014/alignments`)
1. `GET /api/2014/alignments`
2. `GET /api/2014/alignments/{index}`

### Backgrounds (`/api/2014/backgrounds`)
1. `GET /api/2014/backgrounds`
2. `GET /api/2014/backgrounds/{index}`
3. `GET /api/2014/backgrounds/{index}/feature`
4. `GET /api/2014/backgrounds/{index}/starting-equipment`
5. `GET /api/2014/backgrounds/{index}/proficiencies`

### Classes (`/api/classes`)
1. `GET /api/classes`
2. `GET /api/classes/{index}`

### Conditions (`/api/conditions`)
1. `GET /api/conditions`
2. `GET /api/conditions/{index}`

### DamageTypes (`/api/2014/damage-types`)
1. `GET /api/2014/damage-types`
2. `GET /api/2014/damage-types/{index}`

### Equipment (`/api/2014/equipment`)
1. `GET /api/2014/equipment`
2. `GET /api/2014/equipment/{index}`
3. `GET /api/2014/equipment/categories`
4. `GET /api/2014/equipment/category/{category}`
5. `GET /api/2014/equipment/weapons`
6. `GET /api/2014/equipment/armor`
7. `GET /api/2014/equipment/gear`
8. `GET /api/2014/equipment/tools`
9. `GET /api/2014/equipment/mounts-vehicles`
10. `GET /api/2014/equipment/search?q=`

### Languages (`/api/languages`)
1. `GET /api/languages`
2. `GET /api/languages/{index}`
3. `GET /api/languages/type/{type}`
4. `GET /api/languages/script/{script}`

### MagicItems (`/api/magicitems`)
1. `GET /api/magicitems`
2. `GET /api/magicitems/{index}`
3. `GET /api/magicitems/category/{category}`
4. `GET /api/magicitems/rarity/{rarity}`
5. `GET /api/magicitems/search?name=`
6. `GET /api/magicitems/{index}/variants`
7. `GET /api/magicitems/attunement/required`
8. `GET /api/magicitems/cursed`
9. `GET /api/magicitems/categories`
10. `GET /api/magicitems/rarities`
11. `GET /api/magicitems/paginated?page=&pageSize=`

### MagicSchools (`/api/magicschools`)
1. `GET /api/magicschools`
2. `GET /api/magicschools/{index}`

### Monsters (`/api/monsters`)
1. `GET /api/monsters`
2. `GET /api/monsters/{index}`

### Proficiencies (`/api/proficiencies`)
1. `GET /api/proficiencies`
2. `GET /api/proficiencies/{index}`
3. `GET /api/proficiencies/type/{type}`
4. `GET /api/proficiencies/class/{classIndex}`
5. `GET /api/proficiencies/race/{raceIndex}`
6. `GET /api/proficiencies/search?name=`
7. `GET /api/proficiencies/categories`

### Races (`/api/races`)
1. `GET /api/races`
2. `GET /api/races/{index}`
3. `GET /api/races/{index}/traits`
4. `GET /api/races/{index}/subraces`
5. `GET /api/races/{index}/languages`
6. `GET /api/races/{index}/ability-bonuses`
7. `GET /api/races/search?name=`
8. `GET /api/races/sizes`
9. `GET /api/races/speed/{minSpeed}`

### Skills (`/api/skills`)
1. `GET /api/skills`
2. `GET /api/skills/{index}`
3. `GET /api/skills/ability/{abilityIndex}`
4. `GET /api/skills/search?name=`
5. `GET /api/skills/search/description?keyword=`
6. `GET /api/skills/ability-scores`
7. `GET /api/skills/grouped-by-ability`
8. `GET /api/skills/physical`
9. `GET /api/skills/mental`
10. `GET /api/skills/social`
11. `GET /api/skills/{index}/examples`
12. `GET /api/skills/{index}/full-description`
13. `GET /api/skills/count`
14. `GET /api/skills/summary`

### Spells (`/api/spells`)
1. `GET /api/spells`
2. `GET /api/spells/{index}`
3. `GET /api/spells/level/{level}`
4. `GET /api/spells/school/{schoolIndex}`
5. `GET /api/spells/classes/{classIndex}`
6. `GET /api/spells/subclasses/{subclassIndex}`
7. `GET /api/spells/ritual`
8. `GET /api/spells/concentration`
9. `GET /api/spells/search?name=`

### Subclasses (`/api/subclasses`)
1. `GET /api/subclasses`
2. `GET /api/subclasses/{index}`
3. `GET /api/subclasses/class/{className}`

### Subraces (`/api/subraces`)
1. `GET /api/subraces`
2. `GET /api/subraces/{index}`
3. `GET /api/subraces/race/{raceName}`
4. `GET /api/subraces/search/{name}`
5. `GET /api/subraces/{index}/total-bonuses`
6. `GET /api/subraces/{index}/traits`

### WeaponProperties (`/api/2014/weaponproperties`)
1. `GET /api/2014/weaponproperties`
2. `GET /api/2014/weaponproperties/{index}`

### Miért igy valosult meg a wiki retegen

- A statikus JSON cache-szeru betoltese gyors olvasast ad es minimalis DB overheadet.
- A route-ok domain orientaltan vannak kialakitva (search, filter, category), ami frontend query mintakhoz jol illeszkedik.
- Tobb kontrollerben eros defensiv parse/error handling van, mert a forrasfajlok heterogenek.

## 5. Adatmodell es relacios dontesek (EF Core)

Kulcsfontossagu modellezesi mintak:
- Composite key-ek kapcsolat-tablanal (pl. chat membership, community membership)
- Restrict delete, ahol ket felhasznalora mutato FK van (friendship, DM), hogy ne keletkezzen veszteseges kaszkad torles
- Cascade ott, ahol szulo-gyerek eletciklus valoban egyutt mozog (VTT session -> map/token/chat/initiative/asset)
- Unique index egyedi invite/reaction kodokra

### Miért igy valosult meg

Ez a modell egyszerre vedi az adatkonzisztenciat es teszi lehetove a skalahato bovitest (uj social feature-ok, uj VTT funkcionalitasok).

## 6. Jelentosebb technikai kockazatok / inkonzisztenciak

1. Vegyes route prefix strategia
- Van `api/2014/...` es sima `api/...` vegpont ugyanabban wiki tartomanyban.

2. Vegyes namespace-ek
- Nemet controller nem `GameApi.Controllers` namespace alatt van.

3. Hardcoded fallback connection string startupban
- Production security szempontbol nem idealis.

4. Auth register/login query param alapu jelszo payload
- Funkcionalisan mukodik, de body alapu DTO endpoint szakmailag tisztabb lenne.

5. Kettos profile-picture endpoint ugyanazon route-on
- Jelenleg jo, de OpenAPI kliensekben konvencio- es toolingfuggo lehet.

## 7. Osszegzes

A backend modularis, domainek szerint jol szeparalt, es valos ideju (SignalR) + klasszikus REST hibridre optimalizalt. A legfontosabb erossegek:
- role- es ownership-tudatos jogosultsagi kontroll
- VTT allapotkezeles REST snapshot + event alapon
- rugalmas, gyorsan bovitheto MapForge graf tarolas
- tartalom-szolgaltatas wiki modulokkal alacsony uzemeltetesi koltseggel

Fejlesztesi iranykent a route/namespace harmonizacio, auth payload konzisztencia es nehany security hardening (pl. fallback secretek eltavolitasa) ajanlott.
