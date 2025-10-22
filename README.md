# inno3
#  Moodle Search Platform

##  Überblick

Dieses Projekt stellt eine vollständige Suchplattform für Moodle-Kurse bereit, bestehend aus:

1. **Moodle (Bitnami Image)** – Lernplattform
2. **MariaDB** – Datenbank für Moodle
3. **MeiliSearch** – Suchindex für Inhalte
4. **MoodleIndexer (C#)** – Extrahiert Kursdaten aus Moodle und sendet sie an MeiliSearch
5. **Angular-Frontend** – Benutzeroberfläche zum Durchsuchen der Kurse

---

##  Projektstruktur

- docker-compose.yml → Startet alle Services
- MoodleIndexer/ → C# Projekt (Indexer)
- frontend/ → Angular App (Suchoberfläche)
- README.md → Dieses File

---

## ️ Voraussetzungen

- Docker & Docker Compose
- Node.js (für das Frontend)
- .NET 8 SDK (nur falls du den Indexer außerhalb von Docker bauen willst)

---

## ▶Start mit Docker

Alle Backend-Komponenten (Moodle, DB, MeiliSearch, Indexer) werden automatisch gestartet.

```bash
docker compose up --build
```
Nach dem Start erreichst du:
+ Moodle → http://localhost:8081
+ MeiliSearch → http://localhost:7700
+ Angular Frontend → http://localhost:4200

## Entwicklungs-Setup
Backend (Indexer)

```bash
cd MoodleIndexer
dotnet run

```


```bash
cd frontend
npm install
ng serve

```

## Datenfluss
+ Moodle speichert Kursdaten in MariaDB.
+ MoodleIndexer ruft die Moodle-API auf, extrahiert Inhalte (PDFs, Texte, HTML …).
+ Die Inhalte werden an MeiliSearch gesendet.
+ Das Angular-Frontend fragt MeiliSearch ab und zeigt Ergebnisse mit Highlighting an.


