# 🔍 Moodle Search – Projektdokumentation & Übergabe

**Stand:** Januar 2026  
**Projekt:** Innovation Lab 3 (Abschluss)  
**Team:** 37

---

## 1. Allgemeine Beschreibung

**Moodle Search** ist eine externe Suchmaschine für die Lernplattform **Moodle**, konzipiert für die Integration in Hochschulportale (z. B. CIS).  
Sie behebt die bekannten Schwächen der Moodle-Standardsuche (langsam, unvollständig, schlecht filterbar).

### 🔑 Kernfunktionen

- **Volltextsuche**  
  Indiziert Kursinhalte, PDFs, Word-Dokumente und ZIP-Archive

- **Sicherheit**  
  Strikte Rollen- und Sichtbarkeitsprüfung  
  → Studierende sehen keine versteckten Dateien

- **Performance**  
  MeiliSearch liefert Suchergebnisse im Millisekunden-Bereich

- **Architektur**  
  Docker-basierte Microservice-Architektur

---

## 2. Software-Architektur

Das System besteht aus **5 Docker-Containern**, die in einem privaten Netzwerk miteinander kommunizieren.

### 🖥️ Frontend (Angular 17)

- Benutzeroberfläche für die Suche
- Läuft in einem **Nginx-Container**
- Kommuniziert **direkt mit der MeiliSearch API** (Client-Side Search)

> ⚠️ **Zukunftsmusik:**  
> Für maximale Sicherheit sollte die Suche langfristig über ein Backend-Proxy erfolgen.

---

### 🧠 Indexer (.NET 9 Service)

Das Herzstück des Backends.

- Direkte Verbindung zur **Moodle MariaDB**
- Zugriff auf das **moodledata-Volume**
- Extrahiert:
    - ZIP-Dateien
    - PDFs
    - Office-Dokumente
- Bereinigt und überträgt die Daten in den **MeiliSearch Index**

---

### 🔎 MeiliSearch (Search Engine)

- Hält den Suchindex
- Stellt eine REST-API für das Frontend bereit
- Speichert Metadaten wie:
    - `visible_for_students` (boolean)

---

### 🎓 Moodle (LMS)

- Standard **Moodle 4.2 Image (Bitnami)**
- Dient ausschließlich als **Datenquelle**

---

### 🗄️ MariaDB (Datenbank)

- Speichert alle Moodle-Daten
- **Single Source of Truth** für den Indexer

---

### 🔄 Datenfluss

### 🔄 Datenfluss

Moodle DB  
→ Indexer (C#)  
→ MeiliSearch  
← Frontend (Angular)

---

## 3. How-To: Development Install

### ✅ Voraussetzungen

- Docker Desktop installiert

---

### Schritt 1: Repository klonen

```bash
git clone https://github.com/DEIN-REPO/moodle-search.git
cd moodle-search
```

---

### Schritt 2: Container starten

```bash
docker compose up --build
```

⏳ **Hinweis:**  
Beim allerersten Start benötigt Moodle ca. **5–8 Minuten** zur Initialisierung.  
Der Indexer wartet automatisch (`Waiting for Moodle...`).

---

### Schritt 3: Test-Daten anlegen

**Moodle Login:**  
👉 http://localhost:8081

- **User:** `user`
- **Passwort:** `bitnami`

**Testdaten erstellen:**
- Kurse anlegen
- Dateien hochladen (PDF, DOCX, ZIP)

---

### Schritt 4: Indexer neu starten (falls nötig)

Falls der Indexer bereits durchgelaufen ist, bevor Daten vorhanden waren:

```bash
docker compose restart indexer
```

---

### Schritt 5: Zugriff simulieren

- **Student:**  
  http://localhost:4200/?token=student123

- **Lehrender:**  
  http://localhost:4200/?token=teacher999

---

## 4. How-To: Production Deploy
*(Leitfaden für Nachfolger – Innovation Lab 1)*

Für den Produktivbetrieb sind folgende Anpassungen **zwingend notwendig**:

---

### 🔐 Environment Variables

- **Niemals Passwörter** in der `docker-compose.yml` speichern
- `.env` Datei auf dem Server anlegen
- Im Compose-File referenzieren:

```yaml
env_file:
  - .env
```

---

### 🌐 Reverse Proxy & SSL

- Frontend **nicht direkt über Port 4200 exposen**
- Einsatz von:
    - **Nginx Reverse Proxy** oder
    - **Traefik**
- SSL/TLS (**HTTPS**) terminieren
- **CORS-Regeln** in MeiliSearch auf die echte Domain beschränken

---

### 🔒 MeiliSearch Security (WICHTIG!)

- Aktuell nutzt das Frontend den **Public Key**

⚠️ **ToDo für das nächste Team:**

- **Tenant Tokens** implementieren
- Backend (**C#**) generiert signierte Tokens
- Token enthält einen fest verdrahteten Filter:

```json
filter: "visible = true"
```
Dadurch kann ein Student den Filter **niemals clientseitig entfernen**.

---

### ⏱️ Indexer-Strategie

**Aktueller Zustand:**
- Indexer läuft **On-Start**

**Empfehlung für Production:**
- Als **CronJob** ausführen  
  **oder (besser):**
- Moodle sendet bei Änderungen einen **Webhook** an den Indexer

---

## ✅ Status

- ✔️ Funktionsfähig
- ✔️ Deployment-fähig
- ⚠️ Sicherheits- und Skalierungsverbesserungen für den Produktivbetrieb empfohlen

