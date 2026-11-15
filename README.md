# Bean me up

**Bean me up** hält deinen Windows-PC wach und schaltet Windows automatisch zwischen hellem und dunklem Design um. Das Programm ist eine einzelne, portable EXE-Datei (ca. 115 KB). Es braucht keine Installation, keine Adminrechte und keine Internetverbindung.

Erstellt von Simon Seidler · powered by Claude

> Früherer Name: *WachHalter*. Einstellungen und Autostart der Vorgängerversion werden beim ersten Start automatisch übernommen.

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
| **Deine Vorliebe** | Wählst du im Automatikmodus selbst Hell oder Dunkel (in Bean me up oder in den Windows-Einstellungen), gilt das bis zum nächsten geplanten Wechsel. |
| **Mit Windows starten** | Trägt Bean me up in den Autostart deines Benutzerkontos ein (ohne Installation). |

## Bedienung

1. **`BeanMeUp.exe` starten.** Ein kleines Fenster erscheint unten rechts.
2. Das Fenster blendet sich nach **12 Sekunden** in den Infobereich der Taskleiste aus. Solange die Maus darüber ist, pausiert der Countdown. Jeder Klick ins Fenster hält es offen.
3. **Klick auf das Tassen-Symbol** im Infobereich (links oder rechts) öffnet das Fenster wieder – auch dann blendet es sich nach 12 Sekunden automatisch aus.
4. Im Fenster:
   - **„Wach halten aktivieren“** bzw. **„beenden“** – oder direkt eine Dauer wählen.
   - **Hell / Dunkel** und den **Modus** wählen.
   - **„Weitere Einstellungen …“** für Uhrzeiten, Standort und ob Apps und/oder Taskleiste umgestellt werden.
   - **?** oder **F1** öffnet die Hilfe, **Esc** oder **✕** blendet aus, **Beenden** (im Fenster) schließt Bean me up.
5. Mit der Maus über einem Element zeigt ein **Tooltip** kurz, was es tut.

Das Symbol ist eine lächelnde Kaffeetasse vor Sonne und Mond in der Abenddämmerung. Im Infobereich zeigt es den Zustand: **wache Augen** = Wach halten aktiv, **verschlafene Augen** = inaktiv.

## Einstellungen & Datenschutz

- Alle Einstellungen stehen in **`BeanMeUp.ini`** neben der EXE (falls der Ordner schreibgeschützt ist: `%AppData%\BeanMeUp\BeanMeUp.ini`).
- Gibt es noch keine `BeanMeUp.ini`, liest Bean me up einmalig die `WachHalter.ini` der Vorgängerversion.
- Bean me up ändert in der Registry nur das Windows-Design (`AppsUseLightTheme`, `SystemUsesLightTheme`) und – falls gewünscht – den Autostart-Eintrag `BeanMeUp` unter `HKCU\...\Run` (ein alter Eintrag `WachHalter` wird dabei ersetzt).
- **Entfernen:** „Mit Windows starten“ ausschalten, Bean me up beenden, EXE und `.ini` löschen.

## Hinweis zu „Aktivität simulieren“

Die Funktion entspricht einem Maus-Jiggler. Auf Firmenrechnern kann sie gegen IT-Richtlinien verstoßen, weil sie die automatische Sperre umgeht. Deshalb ist sie standardmäßig ausgeschaltet.

## Systemvoraussetzungen

Windows 10 oder 11. .NET Framework 4.8 ist dort bereits enthalten.

---

## Für Entwickler

### Projekt öffnen und bauen

- `BeanMeUp.sln` in **Visual Studio 2022/2026** öffnen.
- **F5** startet mit Debugger, **Erstellen › Projektmappe erstellen** baut.
- **Erstellen › Auswahl veröffentlichen** mit dem Profil **Portable** erzeugt die EXE in `BeanMeUp\bin\Portable\`.
- Alternativ per Kommandozeile:

  ```bash
  dotnet publish BeanMeUp\BeanMeUp.csproj -p:PublishProfile=Portable
  ```

### Aufbau

| Datei | Inhalt |
|---|---|
| `Program.cs` | Einstieg, nur eine Instanz (Hinweis beim zweiten Start), Icon-Export |
| `TrayApp.cs` | Tray-Symbol, Ablaufsteuerung, App-Name |
| `MainWindow.cs` | Hauptfenster (im Code gezeichnet) |
| `SettingsForm.cs` | „Weitere Einstellungen“ |
| `HelpForm.cs` | „Hilfe & Info“ |
| `FancyControls.cs` | Eigene Buttons und Schalter im Windows-11-Stil |
| `Art.cs` | Farben, App-Icon, Sonne/Mond, `.ico`-Erzeugung |
| `KeepAwake.cs` | Wach halten (`SetThreadExecutionState`) und Aktivität simulieren |
| `Theme.cs` | Hell/Dunkel lesen und setzen, Zeitplan, Vorliebe |
| `SunCalc.cs` | Sonnenauf-/-untergang (offline) |
| `AppSettings.cs` | Einstellungen (`.ini`), Autostart, Übernahme aus WachHalter |

Die Fenster sind komplett im Code aufgebaut. Visual Studio öffnet sie deshalb in der Codeansicht statt im Formular-Designer.

### Programm-Icon neu erzeugen

Das Icon wird im Code gezeichnet (`Art.DrawAppIcon`). Nach Änderungen daran:

```bash
BeanMeUp\bin\Release\net48\BeanMeUp.exe --icon-export BeanMeUp\BeanMeUp.ico
```

---

© 2026 Simon Seidler · powered by Claude
