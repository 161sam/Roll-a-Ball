🧠 Codex Entwickler-Prompt: Tiefenanalyse & TODO-Kommentare

🔍 Zielsetzung: Projektweite Tiefenanalyse mit Fokus auf Wartbarkeit

1. Analysiere systematisch den gesamten Quellcode, insbesondere in folgenden Verzeichnissen:

Assets/Material
Assets/OSMAssets/*
Assets/Prefabs
Assets/Resources/*
Assets/Scenes/*
Assets/ScriptableObjects/*
Assets/Scripts/*


2. Identifiziere Verbesserungspotenzial im bestehenden Code:

Unsaubere Logik oder Code-Smells

Harte Codierungen (z. B. Zahlenwerte ohne Kontext)

Unbenutzter Code, leere Methoden, nicht verwendete Felder

Komplexe oder schlecht verständliche Funktionen

Fehleranfällige Konstruktionen (z. B. keine Null-Prüfungen, fehlende Try/Catch-Blöcke)

Unvollständige oder temporäre Lösungen


3. Ergänze an relevanten Stellen prägnante // TODO:-Kommentare.

Kommentare sollen klar verständlich und lösungsorientiert sein.

Beispiel:

// TODO: Magic Number durch benannte Konstante ersetzen

---

🛠️ Weitere Anforderungen

Kommentare sollen nicht bloß allgemeine Hinweise sein, sondern konkrete Aufgaben oder Verbesserungsvorschläge beschreiben.

Wenn eine Stelle kritisch ist, nutze ergänzend // FIXME: oder // WARNING:.

Verändere die Funktionalität nicht – kommentiere nur.

Dokumentiere optional alle hinzugefügten TODOs in einer separaten Datei CodeReview_TODOs.md mit Dateipfad und Zeilennummer.

---

📌 Ziel

Nach Abschluss dieses Prompts ist der gesamte analysierte Code mit gezielten // TODO: Kommentaren angereichert. Dadurch wird klar sichtbar, wo künftig Refactorings, Verbesserungen oder Tests erforderlich sind. Diese Kommentare bilden die Grundlage für eine strukturierte Weiterentwicklung des Projekts.
