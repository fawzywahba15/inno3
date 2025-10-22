#  MoodleIndexer – Kursindexierung aus Moodle nach MeiliSearch

##  Überblick

**MoodleIndexer** ist ein C#-Konsolenprogramm, das Kursdaten aus einer Moodle-Instanz extrahiert und an einen **MeiliSearch**-Index sendet.  
Das Skript dient als Backend-Komponente für eine Suchfunktion im Frontend (Angular), um Moodle-Kurse schnell zu durchsuchen.

---

##  Architektur

Das Projekt besteht aus folgenden Kernkomponenten:

| Ordner | Beschreibung |
|--------|---------------|
| **Models/** | Enthält Datenmodelle (z. B. `Course.cs`) |
| **Services/** | Enthält alle Extraktoren (PDF, Word, HTML, JSON …), `MoodleService` und `IndexService` |
| **Utils/** | Hilfsklassen, z. B. `HtmlUtils.cs` |
| **Program.cs** | Einstiegspunkt – lädt Konfiguration, ruft Moodle-API auf, extrahiert Inhalte und sendet sie an MeiliSearch |

---

##  Konfiguration (`appsettings.json`)

```json
{
  "Database": {
    "Host": "localhost",
    "User": "moodle",
    "Password": "moodlepass",
    "Database": "bitnami_moodle",
    "Port": 3306
  },
  "MeiliSearch": {
    "Url": "http://localhost:7700/indexes/courses/documents",
    "ApiKey": "key123"
  },
  "Moodle": {
    "BaseUrl": "http://localhost:8081",
    "Token": "5f182c03b2dfa5b6c0835a5d3a4dbbb4",
    "FileDataPath": "C:/Users/Fawzy/moodledata/filedir"
  }
}
```
 Wenn das Projekt in Docker läuft, sollten die Hostnamen zu den Servicenamen aus docker-compose.yml geändert werden, z. B.
Host = "mariadb", BaseUrl = "http://moodle:8080", Url = "http://meilisearch:7700/...“

---

##  Funktionsweise


1. Kurse abrufen:
MoodleService verbindet sich mit der Moodle-API, liest Kursdaten und Metainformationen.

2. Dateien analysieren:
Inhalte (z. B. PDFs, Docs, Texte) werden von den jeweiligen Extractor-Klassen verarbeitet.

3. Indexierung:
IndexService sendet die extrahierten Kursdaten als JSON-Dokumente an MeiliSearch.

4. Ausgabe:
Fortschritt und Statusmeldungen werden in der Konsole angezeigt.

##  Startbefehl

```bash 
dotnet run
```

##  Ausführung in Docker
Wenn du das Skript in einen Container einbauen möchtest, lege eine Dockerfile im Projektverzeichnis an:

```
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "MoodleIndexer.dll"]

```
Dann:

```bash
docker build -t moodle-indexer .
docker run --rm --network=host moodle-indexer

```
