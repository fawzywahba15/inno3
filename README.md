# Moodle Search Platform (Inno Lab 3)

## Überblick
Dieses Projekt ist eine Weiterentwicklung der Moodle-Suchplattform. Der Fokus in diesem Semester lag auf der Integration in das CIS-System, der Implementierung von Sicherheitslogik (Rollen & Sichtbarkeit) sowie einer stabilen Docker-Umgebung.

## Features (Sprint 1–5)
* **CIS Integration:** Simulation der iFrame-Einbettung via Token-Parameter.
* **Role Management:** Unterscheidung zwischen **Student** (eingeschränkt) und **Lecturer** (Vollzugriff).
* **Visibility Logic:** In Moodle als "Hidden for students" markierte Dateien werden für Studenten automatisch aus den Suchergebnissen gefiltert.
* **Advanced Indexing:** Unterstützung für PDF, Word, PowerPoint, Excel und ZIP-Archive (rekursive Extraktion).
* **1-Click Deployment:** Vollständiges Setup via Docker Compose.

## Tech Stack
* **Frontend:** Angular (Standalone Components), Material Design
* **Backend:** .NET 9 (C# Indexer Service)
* **Search Engine:** MeiliSearch
* **LMS:** Moodle 4.2 (Legacy Image für Stabilität)
* **Datenbank:** MariaDB 10.6 (LTS)

## Voraussetzungen
* Docker Desktop (muss laufen)
* Keine lokale Installation von .NET oder Node.js notwendig

---

## Start & Installation

Das gesamte System lässt sich mit einem einzigen Befehl starten:

```bash
docker compose up --build
```

Wichtiger Hinweis: Beim allerersten Start benötigt Moodle ca. 5–8 Minuten, um die Datenbank zu initialisieren.Der moodle_indexer wird in dieser Zeit mehrmals neu starten oder warten (Log: Waiting for Moodle...).Das ist normal. Sobald im Log „Moodle setup finished!“ erscheint, ist das System einsatzbereit.

---

## Test-Szenarien (Demo)

### 1. Daten in Moodle anlegen
Öffne Moodle unter: [http://localhost:8081](http://localhost:8081)

* **Login:** `user`
* **Passwort:** `bitnami`

**Aktion:** Erstelle einen Kurs und lade zwei Dateien hoch:
1. **Datei A** (z. B. *Skript.pdf*) – sichtbar lassen.
2. **Datei B** (z. B. *Klausur.pdf*) – verbergen (*Edit mode* → *Hide*).

*Falls der automatische Intervall noch nicht lief, Indexierung manuell auslösen:*

```bash
docker compose restart indexer

```
### 2. Rollen & Sichtbarkeit testen (Frontend)
Die App simuliert den Aufruf aus dem CIS mithilfe von URL-Parametern (`token` & `role`).

**Szenario A: Studierender**
* **URL:** [http://localhost:4200/?token=123&role=student](http://localhost:4200/?token=123&role=student)
* **Erwartung:** Oben rechts steht „Hallo Studierender“. Die versteckte Datei (Datei B) wird **nicht** gefunden.

**Szenario B: Lehrender**
* **URL:** [http://localhost:4200/?token=123&role=lecturer](http://localhost:4200/?token=123&role=lecturer)
* **Erwartung:** Oben rechts steht „Hallo Lehrender“. **Alle** Dateien (A und B) werden gefunden.
---

## Projektstruktur

| Service | URL / Pfad | Beschreibung |
| :--- | :--- | :--- |
| **Frontend** | [http://localhost:4200](http://localhost:4200) | Angular App (Suchoberfläche) |
| **Moodle** | [http://localhost:8081](http://localhost:8081) | Lernplattform (Login: user / bitnami) |
| **MeiliSearch** | [http://localhost:7700](http://localhost:7700) | Suchmaschinen-Dashboard (Key im docker-compose) |
| **Indexer** | `/MoodleIndexer` | C# Backend Service (.NET 9) |
| **Docker** | `docker-compose.yml` | Orchestrierung aller Services |

---

## Troubleshooting

**Fehler: Connection refused im Indexer**
* **Ursache:** Moodle ist noch nicht vollständig installiert.
* **Lösung:** Einige Minuten warten und Logs prüfen.

**Fehler: Suche liefert keine Ergebnisse, obwohl Dateien existieren**
* **Ursache:** Indexer lief, bevor Moodle fertig war.
* **Lösung:**
```bash
docker compose restart indexer
```
## System zurücksetzen

**Achtung:** Dieser Befehl löscht alle Moodle-Daten!

```bash
docker compose down -v