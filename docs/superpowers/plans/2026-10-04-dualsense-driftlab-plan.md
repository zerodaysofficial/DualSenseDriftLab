# DualSense Drift Lab Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Realizzare un programma Windows che misuri il difetto dello stick sinistro del DualSense standard, tenti una correzione temporanea verificabile e permetta salvataggio, Restore e riavvio.

**Architecture:** Core e analisi indipendenti dal sistema operativo, trasporto HID Windows, protocollo DualSense separato e interfaccia Windows Forms. Una sessione centralizzata serializza le operazioni e abilita il salvataggio solo dopo backup e verifica positiva.

**Tech Stack:** C#, .NET 10, Windows Forms, API HID/SetupAPI/Kernel32, System.Text.Json. Test di comportamento in un eseguibile console senza dipendenze NuGet di test.

**Spec:** `docs/superpowers/specs/2026-10-04-dualsense-driftlab-design.md`

## Global Constraints

- Interfaccia italiana; Windows x64; controller Sony standard originale; uso successivo anche su PS5.
- Nessuna sostituzione dei driver, nessun filtro limitato alla GUI presentato come correzione del controller.
- Riposo iniziale per 10 secondi; tre osservazioni da 8 secondi dopo rilascio dal basso; controllo da alto, destra e sinistra.
- Centro accettabile entro 0,02 per asse; variazione tra prima e ultima finestra entro 0,015; ampiezza tra percentili 5 e 95 entro 0,025; finestre di un secondo.
- Almeno 200 campioni distribuiti in ogni periodo di osservazione da 8 secondi; nessuna lacuna superiore a 250 ms.
- Backup valido prima di qualunque scrittura; stesso dispositivo e stesso firmware per il ripristino.
- Salvataggio permanente esplicito, verifica anche dello stick destro e della corsa completa.
- Il riavvio cerca lo stesso dispositivo per un massimo di 15 secondi.
- Test sintetici, compilazione e prova fisica devono essere dichiarati separatamente.

## Review Focus

- Report Bluetooth e interfacce HID secondarie dello stesso controller: non devono essere trattati come connessione USB calibrabile; test in Task 2.
- Una sessione sostituita da un controller ricollegato durante il test: il risultato precedente non deve autorizzare una scrittura; test in Task 6.
- Backup con campi JSON duplicati, numeri fuori limite o seriale contenente caratteri di percorso: importazione rifiutata o percorso neutralizzato; test in Task 5.
- Nessun campione fresco mentre un trasferimento HID si blocca: timeout e nessuna seconda scrittura concorrente; test in Task 2 e Task 6.
- Chiusura della finestra, annullamento o cambio dispositivo durante una scrittura persistente: una sola operazione conclude o fallisce con stato esplicito; test in Task 6 e Task 7.

## File Structure

`DualSenseDriftLab.sln`; `src/DriftLab.Core/` per modelli, analisi e test guidato; `src/DriftLab.Protocol/` per il protocollo e le operazioni; `src/DriftLab.WindowsHid/` per le API native; `src/DriftLab.Storage/` per backup e cronologia; `src/DriftLab.Desktop/` per sessione e GUI; `tests/DriftLab.Tests/` per il runner e le prove di comportamento; `docs/protocol-reference.md`, `docs/hardware-checklist.md`, `README_IT.md`, `THIRD_PARTY_NOTICES.md` per fonti e utilizzo.

I progetti Core, Protocol, Storage e Tests usano `net10.0`. WindowsHid e Desktop usano `net10.0-windows`, Desktop attiva Windows Forms ed entrambi consentono il targeting Windows durante la compilazione da Linux. Prima delle scritture si deve poter eseguire almeno la suite indipendente da Windows.

## Task 1: Identità, payload e provenienza del protocollo

**Files:** `src/DriftLab.Core/Models.cs`, `src/DriftLab.Protocol/FeatureCodec.cs`, `src/DriftLab.Protocol/IHidTransport.cs`, `tests/DriftLab.Tests/ProtocolTests.cs`, i rispettivi `.csproj`, `tests/DriftLab.Tests/Program.cs`, `docs/protocol-reference.md`, `THIRD_PARTY_NOTICES.md`.

**Interfaces:** Produce `ControllerIdentity(string Serial, uint Firmware, uint Hardware)`, `StickSample(TimeSpan Time, double LX, double LY, double RX, double RY)`, `CalibrationData(ushort[] Values)` validato a 12 valori. `IHidTransport` espone `Task<byte[]> GetFeatureAsync(byte reportId, CancellationToken ct)`, `Task SetFeatureAsync(byte reportId, ReadOnlyMemory<byte> payload, CancellationToken ct)` e `IAsyncEnumerable<byte[]> ReadInputsAsync(CancellationToken ct)`; i feature report letti includono il report ID, quelli scritti ricevono ID separato dal payload. Il runner Tests seleziona i gruppi dal primo argomento e termina con codice diverso da zero se almeno un'asserzione fallisce.

- [ ] Scrivere `ReadCalibrationRejectsWrongHeader`, `FinetunePreservesUnchangedValues`, `SerialRejectsErrorPlaceholder`, `UnknownNvStatusDisablesWrites`: aspettarsi eccezione sul report errato, 12 parole little-endian corrette e nessuna identità valida per un errore.
- [ ] Eseguire `dotnet run --project tests/DriftLab.Tests -- Protocol`; deve fallire per le interfacce mancanti, senza errori di infrastruttura.
- [ ] Definire i modelli e il codec; usare il riferimento dualshock-tools alla revisione `fbbe58d55636ee9b81b71f9aaebba3fd8956a105`. Verificare `js/controllers/ds5-controller.js`, `js/controllers/base-controller.js`, `js/modals/finetune-modal.js` e `LICENSE.txt`. Registrare revisione, origine, licenza e hash dei file consultati.
- [ ] Fissare questi payload verificati: lettura calibrazione ID `0x80`, `[12,2]`, risposta `0x81` con 12 ushort da offset 4; scrittura `[12,1]` più 24 byte little-endian; ordine `LL,LT,RL,RT,LR,LB,RR,RB,LX,LY,RX,RY`; centro sinistro agli indici 8 e 9. Riavvio `[1,1]`; unlock `[3,2,101,50,64,12]`; lock `[3,1]`; query NVS `[3,3]`. Verificare risposte e limiti prima dell'uso, senza copiare la gestione degli errori permissiva dell'interfaccia web.
- [ ] Rieseguire il gruppo Protocol: tutte le prove devono passare; creare la soluzione e aggiungere i progetti.
- [ ] Committare `feat: define validated DualSense protocol and models`.

## Task 2: Connessione USB e letture reali

**Files:** `src/DriftLab.WindowsHid/NativeMethods.cs`, `HidDiscovery.cs`, `WindowsHidTransport.cs`; `src/DriftLab.Protocol/UsbInputDecoder.cs`; `tests/DriftLab.Tests/InputTests.cs`.

**Interfaces:** Consuma i modelli e `IHidTransport`. Produce `IReadOnlyList<HidDeviceInfo> HidDiscovery.FindDualSenseUsb()`, `Task<WindowsHidTransport> OpenAsync(HidDeviceInfo device, CancellationToken ct)`, `bool UsbInputDecoder.TryDecode(ReadOnlySpan<byte> report, TimeSpan time, out StickSample sample, out BatteryInfo battery)`. `HidDeviceInfo` conserva percorso e lunghezze HID; `BatteryInfo` distingue valore sconosciuto, carica, stato USB ed errore.

- [ ] Scrivere test per assi 0/127/128/255, report corto, ID errato, report Bluetooth, interface collection diversa, dati batteria mancanti e perdita di campioni. Le letture devono mantenere il segno verticale originale e non applicare deadzone.
- [ ] Eseguire `dotnet run --project tests/DriftLab.Tests -- Input`; deve fallire per decoder mancante.
- [ ] Implementare decoder e scoperta Sony VID `0x054C`, DualSense standard PID `0x0CE6`, collection gamepad, verificando USB dalle capacità e dal report; rifiutare Edge e Bluetooth per le scritture. Normalizzazione simmetrica `(raw - 127.5) / 127.5`.
- [ ] Implementare handle sicuri, enumerazione x64 corretta, cancellazione delle letture pendenti e lunghezze feature ottenute dal dispositivo. Un solo trasferimento feature alla volta; un timeout non autorizza una seconda chiamata nativa finché la prima non è conclusa o la sessione è invalidata.
- [ ] Eseguire suite Input e build WindowsHid; l'accesso USB fisico resta una prova distinta su Windows.
- [ ] Committare `feat: read genuine DualSense USB input through Windows HID`.

## Task 3: Test guidato e classificazione del difetto

**Files:** `src/DriftLab.Core/DriftAnalyzer.cs`, `GuidedTest.cs`, `AnalysisOptions.cs`; `tests/DriftLab.Tests/AnalysisTests.cs`, `GuidedTestTests.cs`.

**Interfaces:** Produce `DriftResult Analyze(IReadOnlyList<StickSample> samples, AnalysisOptions options)` con statistiche e classificazione. `GuidedTest.Accept(StickSample sample)` restituisce `GuidedTestUpdate`; `GuidedTest.Cancel()` conclude senza un risultato valido. `GuidedTestResult` conserva generazione della sessione, campioni del riposo, tre risultati dal basso, tre risultati dalle altre direzioni e un esito complessivo. `GuidedTestUpdate` contiene fase, istruzione italiana, progresso ed eventuale `GuidedTestResult` conclusivo.

- [ ] Scrivere prove per offset stabile di 0,06, progressione verticale da 0 a -0,08 in 8 secondi, rumore, direzione di rilascio, 199 campioni, lacuna di 251 ms, mancato rientro e movimento durante la registrazione. Assert: instabilità e dati insufficienti non diventano errore stabile correggibile.
- [ ] Eseguire `dotnet run --project tests/DriftLab.Tests -- Analysis` e `-- Guided`; entrambi devono fallire prima dell'implementazione.
- [ ] Implementare mediane, percentili, finestre temporali e soglie della specifica. Rilevare l'arrivo in basso a LY >= 0,8 e il rilascio nella zona |LX|,|LY| <= 0,25; attendere 500 ms di assestamento, registrare 8 secondi. Dopo 15 secondi senza rientro, esito mancato rientro; un movimento >0,25 durante l'osservazione invalida il ciclo. Ripetere i cicli invalidi soltanto su azione dell'utente.
- [ ] Implementare riposo 10 secondi e controlli da alto/destra/sinistra con osservazione 8 secondi ciascuno. Distribuire i 200 campioni sull'intero intervallo; dati privi delle finestre iniziale o finale sono insufficienti.
- [ ] Rieseguire i due gruppi; valori esattamente alla soglia devono essere accettati e quelli superiori rifiutati.
- [ ] Committare `feat: detect repeatable stick drift after directional release`.

## Task 4: Correzione temporanea e conferma mediante misure

**Files:** `src/DriftLab.Protocol/DualSenseController.cs`, `src/DriftLab.Core/CenterCorrector.cs`, `VerificationPolicy.cs`; `tests/DriftLab.Tests/CorrectionTests.cs`.

**Interfaces:** `DualSenseController` espone `Task<ControllerIdentity> ReadIdentityAsync(CancellationToken ct)`, `Task<CalibrationData> ReadCalibrationAsync(CancellationToken ct)`, `Task WriteTemporaryAsync(CalibrationData data, CancellationToken ct)`, `Task SavePermanentAsync(CancellationToken ct)`, `Task RebootAsync(CancellationToken ct)`, `Task<NvStatus> QueryNvStatusAsync(CancellationToken ct)`. `NvStatus` distingue locked, unlocked, pending-reboot e unknown. `Task<CorrectionAttempt> CenterCorrector.CorrectAsync(CalibrationData original, DriftResult stableResult, ICalibrationSession session, CancellationToken ct)` restituisce dati candidati, convergenza, numero di scritture ed esito del ripristino. `ICalibrationSession` espone `Task WriteTemporaryAsync(CalibrationData data, CancellationToken ct)` e `Task<IReadOnlyList<StickSample>> GetFreshSamplesAsync(TimeSpan duration, CancellationToken ct)`. `VerificationPolicy.Evaluate(GuidedTestResult before, GuidedTestResult after, RangeCheckResult rangeCheck)` restituisce `VerificationResult` con esito e permesso di salvare; `RangeCheckResult` conserva estremi e rientro di entrambi gli stick.

- [ ] Scrivere prove: il feedback sintetico con entrambi i versi di risposta converge; plateau e segnale variabile falliscono; centro destro e parametri di corsa restano identici; miglioramento parziale e corsa ridotta non permettono salvataggio.
- [ ] Eseguire `dotnet run --project tests/DriftLab.Tests -- Correction`; deve fallire.
- [ ] Implementare un unico tentativo limitato ai centri sinistri, con misure reali dopo ogni scrittura: perturbazione iniziale di 32 unità per osservare il verso di risposta, ricerca nel limite originale +/-500, massimo 10 candidati per asse e 500 ms di dati freschi per candidato. La mediana dei report non viene mai usata direttamente come parametro raw. Variabilità, assenza di risposta o aumento dell'errore interrompono il tentativo e ripristinano il backup temporaneo.
- [ ] Separare rigorosamente questa ricerca temporanea dal salvataggio NVS; nessun candidato sblocca la memoria persistente. Sul segnale instabile offrire solo il tentativo guidato del centro previsto dalla specifica, annunciando che coinvolge entrambi gli stick; non avviarlo automaticamente.
- [ ] Verificare readback dei 12 valori dopo le scritture. Ripetere l'intero test dopo la correzione e chiedere corsa completa di entrambi gli stick, con estremi per asse >=0,95 e rientro valido. Fallimento del ripristino resta visibile.
- [ ] Rieseguire Correction e Protocol; committare `feat: attempt bounded temporary center correction and verify outcome`.

## Task 5: Backup e cronologia ripristinabile

**Files:** `src/DriftLab.Storage/CalibrationBackup.cs`, `BackupRepository.cs`, `BackupValidator.cs`; `tests/DriftLab.Tests/StorageTests.cs`.

**Interfaces:** `Task SaveAsync(CalibrationBackup backup, CancellationToken ct)`, `Task<CalibrationBackup> ReadAsync(string path, CancellationToken ct)`, `Task<IReadOnlyList<CalibrationBackup>> ListAsync(ControllerIdentity identity, CancellationToken ct)`; `BackupValidator.Validate(backup, connectedIdentity)` deve fallire prima della scrittura per contenuto incompatibile.

- [ ] Scrivere prove di JSON corrotto, campi duplicati, oltre 12 valori, overflow ushort, identità diversa, firmware diverso, hash errato e errore di disco che preserva il precedente file valido.
- [ ] Eseguire `dotnet run --project tests/DriftLab.Tests -- Storage`; deve fallire.
- [ ] Implementare formato versione 1, hash SHA-256 del payload canonico, limite di importazione 64 KiB, nomi cartella derivati da hash dell'identità. Conservare seriali come dati, mai come segmenti di percorso. Scrivere file temporaneo nella stessa cartella, flush e sostituzione atomica.
- [ ] Separare backup di sessione da cronologia permanente; Restore applica prima in modo temporaneo e verifica il readback. Un backup non viene etichettato come calibrazione di fabbrica.
- [ ] Rieseguire Storage; committare `feat: preserve identity-bound calibration backups and restore history`.

## Task 6: Sessione e autorizzazione delle operazioni

**Files:** `src/DriftLab.Core/SessionState.cs`, `src/DriftLab.Protocol/ControllerSession.cs`; `tests/DriftLab.Tests/SessionTests.cs`. La sessione appartiene al progetto Protocol, indipendente da Windows, per poter provare l'intera orchestrazione con un trasporto controllato.

**Interfaces:** `SessionState.CanSave`, `CanRestore`, `CanReboot`, `IsPersistentWrite`; ogni connessione riceve una nuova generazione. `ControllerSession` espone `Task RunAutoAsync(CancellationToken ct)`, `Task SaveAsync(CancellationToken ct)`, `Task RestoreAsync(CalibrationBackup backup, CancellationToken ct)` e `Task RebootAsync(CancellationToken ct)`, con `event Action<SessionSnapshot> Updated`. Consuma controller, repository e interfaccia `IUserPrompts` per conferme e avanzamento del test guidato; `IUserPrompts` espone `Task<bool> ConfirmAsync(string text, CancellationToken ct)`. Lo snapshot contiene stato, identità, istruzione, ultimo campione e risultati disponibili. I permessi derivano dalla policy Core, non dai pulsanti.

- [ ] Scrivere prove di risultati scaduti dopo disconnessione, doppio click, backup fallito, timeout HID, chiusura o annullamento durante salvataggio, controller diverso dopo reboot e readback persistente diverso dal valore richiesto.
- [ ] Eseguire `dotnet run --project tests/DriftLab.Tests -- Session`; deve fallire.
- [ ] Implementare sequenza backup -> test -> eventuale correzione -> verifica. Abilitare save solo per stessa generazione e test completo superato; conservare una sola operazione mutante attiva. Snapshot con batteria sconosciuta richiede controllo esplicito dell'utente; errore batteria blocca scrittura.
- [ ] Implementare unlock/lock con finally e messaggio distinto se il blocco finale fallisce; il tentativo finale di lock usa un token di recupero distinto da quello eventualmente annullato dall'utente, rispettando la serializzazione del trasporto. Registrare salvataggio confermato soltanto dopo riavvio, riconnessione dello stesso seriale entro 15 secondi e readback identico. Se non è verificato, mostrare stato incerto e impedire un'altra scrittura automatica.
- [ ] Rieseguire Session e tutta la suite; committare `feat: enforce safe calibration session transitions`.

## Task 7: Interfaccia Windows completa

**Files:** `src/DriftLab.Desktop/Program.cs`, `MainForm.cs`, `StickView.cs`, `TimelineView.cs`, `HistoryDialog.cs`.

**Interfaces:** Consuma `ControllerSession` e snapshot. I controlli visualizzano i dati effettivi e inviano soltanto azioni; disegno dei grafici a frequenza limitata, acquisizione e analisi non dipendono dal repaint.

- [ ] Definire una verifica funzionale della GUI: collegamento/assenza controller, selezione di più dispositivi, istruzioni per ogni fase, risultato prima/dopo, Restore, import/export e chiusura durante salvataggio; assert sui permessi in SessionTests per le azioni raggiungibili.
- [ ] Implementare finestra italiana con due stick, grafico verticale, istruzione corrente, progressione, metriche e pulsanti della specifica. Il pulsante principale è **Test e correzione automatica**; i messaggi distinguono miglioramento parziale, instabilità e test superato.
- [ ] Implementare notifiche USB e riconnessione senza bloccare la GUI. La chiusura resta trattenuta durante la scrittura persistente; la finestra spiega l'operazione in corso.
- [ ] Eseguire `dotnet build src/DriftLab.Desktop -c Release`; verificare su Windows l'interfaccia in assenza di controller e con controller reale. Se non c'è Windows disponibile, registrare esplicitamente che la prova visiva non è stata eseguita.
- [ ] Committare `feat: provide Italian DualSense drift diagnosis desktop interface`.

## Task 8: Pacchetto e verifica conclusiva

**Files:** `README_IT.md`, `docs/hardware-checklist.md`, archivio sorgenti e directory `dist/win-x64/` del pacchetto effettivamente pubblicato.

- [ ] Eseguire `dotnet run --project tests/DriftLab.Tests -c Release`: tutti i gruppi devono passare.
- [ ] Eseguire `dotnet publish src/DriftLab.Desktop -c Release -r win-x64 --self-contained true -o dist/win-x64`; verificare formato PE dell'eseguibile, dipendenze incluse e contenuto del pacchetto. Se SDK o targeting pack non sono ottenibili, non sostituire una compilazione reale con un file rinominato.
- [ ] Documentare uso e risultati verificati, procedura di prova USB su Windows, salvataggio dopo riavvio e Restore. La checklist fisica include la riproduzione del difetto dell'utente e prova sulla PS5 dopo salvataggio.
- [ ] Richiedere revisione finale del ramo secondo il metodo scelto, correggere i rilievi rilevanti e rieseguire i controlli interessati.
- [ ] Committare `docs: package DualSense Drift Lab and document verified limits`; creare ZIP dei sorgenti da file tracciati, escludendo git, cache, segreti e output intermedi. Salvare gli artefatti finali e consegnare con link distinti e stato veritiero delle prove hardware.

## Execution Handoff

Esecuzione consigliata: **diretta nella sessione**, con revisione finale indipendente, perché trasporto, protocollo e sessione condividono interfacce strette e conviene validarli insieme. Alternativa: implementazione per task con agenti e revisori separati, con più passaggi e costo superiore.

La specifica è stata approvata. Prima dell'implementazione, l'utente deve revisionare questo piano e scegliere il metodo di esecuzione. La compilazione non è ancora stata effettuata; questo ambiente attualmente non espone un comando `dotnet`, quindi il primo controllo esecutivo riguarda la disponibilità del toolchain.
