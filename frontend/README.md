#  Moodle Search Frontend

## Überblick

Dieses Angular-Frontend stellt eine Suchoberfläche für Moodle-Kurse bereit.  
Es kommuniziert mit einer **MeiliSearch**-Instanz, die durch das C#-Projekt *MoodleIndexer* befüllt wird.

---

## ⚙ Aufbau

| Bereich | Beschreibung |
|----------|---------------|
| **services/search.service.ts** | Stellt Anfragen an MeiliSearch über HTTP |
| **components/search/** | UI-Komponente für die Kurssuche |
| **models/course.model.ts** | Datendefinitionen für Kurse, Dateien, Seiten usw. |
| **utils/utils.ts** | Hilfsfunktionen (HTML-Strip, Dateigröße-Formatierung) |

---

##  Konfiguration

Der Standard-Endpunkt in `search.service.ts` lautet:

```typescript
private readonly API_URL = 'http://localhost:7700/indexes/courses/search';
private readonly headers = new HttpHeaders({
  'Content-Type': 'application/json',
  Authorization: 'Bearer key123',
});
```

## Entwicklung starten

```bash
npm install
ng serve
```
## Funktionsweise

+ Nutzer gibt Suchbegriff ein → Anfrage an MeiliSearch

+ Ergebnisse werden mit Highlighting dargestellt

+ Filterung und Sortierung nach Kursdaten möglich

+ Unterstützt verschiedene Inhaltsarten (Sektionen, Seiten, Bücher, Dateien)
