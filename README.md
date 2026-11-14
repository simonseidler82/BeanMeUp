# WachHalter

**WachHalter** hält deinen Windows-PC wach und schaltet Windows automatisch zwischen hellem und dunklem Design um. Das Programm ist eine einzelne, portable EXE-Datei (ca. 90 KB). Es braucht keine Installation, keine Adminrechte und keine Internetverbindung.

Erstellt von Simon Seidler · powered by Claude

---

## Funktionen

| Bereich | Was es tut |
|---|---|
| **Wach halten** | Verhindert Energiesparmodus und Ruhezustand – ohne Zeitlimit (∞) oder für 30 Min., 1, 2 oder 4 Std. |
| **Bildschirm eingeschaltet lassen** | Verhindert zusätzlich, dass sich der Monitor ausschaltet. |
| **Aktivität simulieren** | Bewegt die Maus nach 25 s ohne Eingabe unsichtbar um 1 Pixel hin und zurück. Verhindert damit auch Bildschirmschoner, automatische Sperre und „Abwesend“ (z. B. Teams). Standardmäßig **aus**. |
| **Hell / Dunkel** | Stellt Windows sofort um. |
| **Modus Zeitplan** | Wechselt zu festen Uhrzeiten (Standard: Hell ab 07:00, Dunkel ab 19:00). |
| **Modus Sonnenstand** | Wechselt bei Sonnenauf- und -untergang für deinen Standort (offline berechnet). |
| **Deine Vorliebe** | Wählst du im Automatikmodus selbst Hell oder Dunkel (in WachHalter oder in den Windows-Einstellungen), gilt das bis zum nächsten geplanten Wechsel. |
| **Mit Windows starten** | Trägt WachHalter in den Autostart deines Benutzerkontos ein (ohne Installation). |

## Bedienung

1. **`WachHalter.exe` starten.** Ein kleines Fenster erscheint unten rechts.
2. Das Fenster blendet sich nach **12 Sekunden** in den Infobereich der Taskleiste aus. Solange die Maus darüber ist, pausiert der Countdown. Jeder Klick ins Fenster hält es offen.
3. **Klick auf das Tassen-Symbol** im Infobereich (links oder rechts) öffnet das Fenster wieder.
4. Im Fenster:
   - **„Wach halten aktivieren“** bzw. **„beenden“** – oder direkt eine Dauer wählen.
   - **Hell / Dunkel** und den **Modus** wählen.
   - **„Weitere Einstellungen …“** für Uhrzeiten, Standort und ob Apps und/oder Taskleiste umgestellt werden.
   - **?** oder **F1** öffnet die Hilfe, **Esc** oder **✕** blendet aus, **Beenden** (im Fenster) schließt WachHalter.
5. Mit der Maus über einem Element zeigt ein **Tooltip** kurz, was es tut.

Das Tray-Symbol zeigt den Zustand: **mit orangem Dampf** = Wach halten aktiv, **blass ohne Dampf** = inaktiv.

## Einstellungen & Datenschutz

- Alle Einstellungen stehen in **`WachHalter.ini`** neben der EXE (falls der Ordner schreibgeschützt ist: `%AppData%\WachHalter\WachHalter.ini`).
- WachHalter ändert in der Registry nur das Windows-Design (`AppsUseLightTheme`, `SystemUsesLightTheme`) und – falls gewünscht – den Autostart-Eintrag unter `HKCU\...\Run`.
- **Entfernen:** „Mit Windows starten“ ausschalten, WachHalter beenden, EXE und `.ini` löschen.

## Hinweis zu „Aktivität simulieren“

Die Funktion entspricht einem Maus-Jiggler. Auf Firmenrechnern kann sie gegen IT-Richtlinien verstoßen, weil sie die automatische Sperre umgeht. Deshalb ist sie standardmäßig ausgeschaltet.

## Systemvoraussetzungen

Windows 10 oder 11. .NET Framework 4.8 ist dort bereits enthalten.

---

## Für Entwickler

### Projekt öffnen und bauen

- `WachHalter.sln` in **Visual Studio 2022/2026** öffnen.
- **F5** startet mit Debugger, **Erstellen › Projektmappe erstellen** baut.
- **Erstellen › Auswahl veröffentlichen** mit dem Profil **Portable** erzeugt die EXE in `WachHalter\bin\Portable\`.
- Alternativ per Kommandozeile:

  ```bash
  dotnet publish WachHalter\WachHalter.csproj -p:PublishProfile=Portable
  ```

### Aufbau

| Datei | Inhalt |
|---|---|
| `Program.cs` | Einstieg, nur eine Instanz (Hinweis beim zweiten Start), Icon-Export |
| `TrayApp.cs` | Tray-Symbol, Ablaufsteuerung |
| `MainWindow.cs` | Hauptfenster (im Code gezeichnet) |
| `SettingsForm.cs` | „Weitere Einstellungen“ |
| `HelpForm.cs` | „Hilfe & Info“ |
| `FancyControls.cs` | Eigene Buttons und Schalter im Windows-11-Stil |
| `Art.cs` | Farben, App-Icon, Sonne/Mond, `.ico`-Erzeugung |
| `KeepAwake.cs` | Wach halten (`SetThreadExecutionState`) und Aktivität simulieren |
| `Theme.cs` | Hell/Dunkel lesen und setzen, Zeitplan, Vorliebe |
| `SunCalc.cs` | Sonnenauf-/-untergang (offline) |
| `AppSettings.cs` | Einstellungen (`.ini`) und Autostart |

Die Fenster sind komplett im Code aufgebaut. Visual Studio öffnet sie deshalb in der Codeansicht statt im Formular-Designer.

### Programm-Icon neu erzeugen

Das Icon wird im Code gezeichnet (`Art.DrawAppIcon`). Nach Änderungen daran:

```bash
WachHalter\bin\Release\net48\WachHalter.exe --icon-export WachHalter\WachHalter.ico
```

---

© 2026 Simon Seidler · powered by Claude
