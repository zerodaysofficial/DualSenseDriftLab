# DualSense Drift Lab — Windows x64 — beta 3

**Download:** [Pacchetto Windows e archivio sorgenti — beta 3](https://github.com/zerodaysofficial/DualSenseDriftLab/releases/tag/v0.1.0-beta.3). In **Assets**, scegli `DualSenseDriftLab_Windows.zip` per avviare il programma.

Programma nativo in inglese e italiano per DualSense Sony **standard** collegato con cavo USB dati. Legge i report reali, guida il test del rientro dello stick sinistro, analizza tutti e quattro gli assi e tenta una calibrazione temporanea. Offre salvataggio nel controller, Restore da backup e riavvio. Non usa un controller virtuale o un filtro che modifica soltanto la grafica.

**Made by zer0day**. Protocollo di calibrazione: **[dualshock-tools](https://github.com/dualshock-tools/dualshock-tools.github.io) · the_al**, licenza MIT e attribuzione incluse in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

**Stato:** compilato per Windows x64, con test software su dati sintetici e trasporto controllato. Non è stato avviato su Windows né provato su un DualSense fisico in questo ambiente. Riconoscimento USB, interfaccia, compatibilità firmware, salvataggio effettivo e comportamento su PS5 richiedono le prove indicate in `hardware-checklist.md`. La calibrazione può correggere uno scostamento del centro; non ripara un sensore usurato o un segnale che continua a cambiare.

## Avvio

1. Chiudi la versione precedente ed estrai **tutta** la cartella del nuovo pacchetto Windows in una cartella nuova. Conserva le DLL e gli altri file accanto all'eseguibile.
2. Apri `DualSenseDriftLab.exe`. La prima schermata è in inglese: **Select your language**, con **English** prima e **Italiano** dopo. Scegli e premi **Continue**. English è preselezionato; la scelta compare a ogni avvio, prima del rilevamento USB. Chiudendo questa schermata il programma termina. Il pacchetto include .NET: non devi installarlo separatamente.
3. Collega il DualSense con un cavo USB dati. Chiudi eventuali app che lo occupano. Non cambiare driver.
4. Premi **Test e correzione automatica** e segui le istruzioni. All'inizio lascia entrambi gli stick liberi per 10 secondi. Poi porta il sinistro in basso e rilascialo tre volte; fai altrettanto da alto, destra e sinistra. Ogni rientro è osservato per 8 secondi.
5. Se rileva un centro stabile spostato, prova una regolazione temporanea del solo centro sinistro. Per tremolii, picchi, segnale instabile o problema anche al destro propone, con conferma, un solo tentativo di calibrazione del centro di **entrambi** gli stick.
6. Dopo il tentativo ripeti il test e ruota entrambi gli stick fino ai bordi, poi rilasciali. Solo una verifica completa positiva abilita **Salva nel controller**.

La lingua scelta si applica a pulsanti, istruzioni, risultati, messaggi del programma e conferme Sì/No. I messaggi e le finestre di sistema Windows possono seguire la lingua del sistema operativo. **Made by zer0day** e i crediti a **dualshock-tools · the_al** sono visibili nella schermata iniziale, nella finestra e nel pulsante **Crediti**. La lingua non modifica il formato o gli hash dei backup precedenti. È incluso anche `README_EN.md`.

La prima procedura completa può durare alcuni minuti. I movimenti volontari o i campioni mancanti invalidano un ciclo. Il programma propone di ripeterlo; non applica una deadzone per far superare la prova.

## Rimuovi tremolio · Hard Detect (ramo sperimentale)

Questa funzione è in sviluppo sul ramo `feature/hard-detect-persistent-calibration` e **non è inclusa nella release beta 3** indicata sopra. Esegue un controllo aggiuntivo degli stick lasciati fermi per 15 secondi, il test guidato dei sei rilasci e una seconda misura di 15 secondi dopo un eventuale tentativo temporaneo di calibrazione. Mostra l'oscillazione senza filtro dei due stick e i campioni con picchi, senza alterare i dati visualizzati.

Il pulsante **non può installare una deadzone o un filtro nel firmware**. Può soltanto cercare di migliorare i parametri di calibrazione disponibili sul DualSense standard. Un tentativo che non modifica i parametri, lascia tremolio o non supera tutti i controlli viene annullato: il salvataggio resta bloccato. Una verifica positiva abilita il consueto **Salva nel controller** con conferma esplicita, backup, riavvio e rilettura. Solo questa scrittura verificata rende permanente la calibrazione, che resta utilizzabile su PS5; l'efficacia sul tremolio in un gioco va comunque verificata fisicamente.

Le misure temporanee non riparano l'usura meccanica né garantiscono la scomparsa di difetti intermittenti. Non sono stati effettuati test con un controller fisico o su PS5, né pubblicato un nuovo eseguibile Windows di questa funzione.

## Salva, Restore e riavvio

Il test e il tentativo automatico non salvano permanentemente. **Salva nel controller** chiede una conferma separata: tieni collegati il cavo e la batteria interna durante l'operazione. La finestra non si chiude e il pulsante Annulla viene disabilitato durante la scrittura. Il successo viene dichiarato solo dopo aver osservato la scomparsa dell’interfaccia HID, la riconnessione dello stesso seriale e una rilettura identica dei parametri con memoria bloccata. Se il riavvio non è osservabile o avviene troppo rapidamente per il controllo, il salvataggio rimane non verificato.

**Restore** sceglie un backup della cronologia, lo applica temporaneamente e ne controlla la rilettura. **Salva Restore** permette di renderlo permanente anche se quel vecchio backup contiene il drift: è una scelta esplicita per tornare alla calibrazione precedente. Non è un ripristino universale di fabbrica.

**Riavvia controller** scarta le modifiche temporanee e cerca lo stesso dispositivo per 15 secondi. Dopo una disconnessione occorre un nuovo test. Un errore di salvataggio lascia lo stato **incerto**, senza dichiarare la calibrazione conservata.

I backup sono in `%LOCALAPPDATA%\DualSenseDriftLab\Backups`. Contengono i 12 parametri di calibrazione, identità, firmware, data e hash SHA-256; non sono copie del firmware. Usa **Esporta backup** per conservarne una copia. **Importa backup** rifiuta seriale/firmware differenti, hash errato, JSON inatteso, campi duplicati e valori fuori intervallo.

## Soglie del test

La beta 2 rileva anche piccoli scostamenti, microtremolii e picchi isolati su entrambi gli stick. Mostra centro X/Y, deriva nel tempo, oscillazione P5–P95, picco massimo e numero di campioni con picchi prima e dopo. Il numero conta campioni per asse, non eventi distinti o frequenza del tremolio.

Sono parametri di progetto, non specifiche Sony:

- Centro accettato entro circa ±0,78% per asse; la regolazione con feedback del sinistro punta a circa ±0,39% di residuo.
- Deriva: differenza tra mediane della prima/ultima finestra superiore a circa 0,78%.
- Microtremolio: ampiezza P5–P95 superiore a un passo USB, circa 0,78%. Oltre 2,5% viene indicato rumore significativo.
- Picchi: campioni distanti dalla mediana più di due passi USB, circa 1,57%. Vengono indicati come picchi intermittenti se rari o nascosti dall’ampiezza percentile; con oscillazioni diffuse può prevalere la diagnosi di rumore o microtremolio. Qualunque campione con picco rilevato impedisce una diagnosi normale e può bloccare la verifica.
- Centro dipendente dal rilascio: differenza tra mediane dei cicli superiore a circa 1,57%.

I report USB rappresentano ogni asse con 256 valori: un passo è circa 0,78% e il centro cade tra due codici, ±0,39%. L'alternanza tra quei due codici non viene scambiata per un tremolio. Lo zero esatto non è rappresentabile in ogni campione. Le soglie includono una piccola tolleranza numerica.

Finestre da 1 secondo, almeno 200 campioni per osservazione da 8 secondi e nessuna lacuna oltre 250 ms. Il controllo della corsa richiede almeno ±95% per tutti e quattro gli assi e rientro entro la nuova tolleranza del centro. Il salvataggio richiede il test completo dopo la calibrazione senza difetti rilevati; se tremolii o picchi restano, ripristina la calibrazione precedente e disabilita il salvataggio. La correzione non filtra né nasconde il segnale. La regolazione con feedback cerca il centro tra valori temporanei entro ±500 unità dei parametri originali, con al massimo 12 scritture per asse: durante la ricerca lo scostamento può aumentare brevemente. Se non trova un centro stabile ripristina i parametri precedenti.

Un test superato riguarda i periodi osservati; non esclude un difetto intermittente successivo o un picco sotto soglia. Non è una riparazione meccanica e non garantisce di eliminare ogni difetto.

## Crediti

**Made by zer0day** — Applicazione Windows, selezione della lingua, test guidati degli stick e diagnostica del drift sviluppati per DualSense Drift Lab.

- **[dualshock-tools](https://github.com/dualshock-tools/dualshock-tools.github.io) · the_al** — Progetto originale usato come riferimento per i comandi di calibrazione DualSense e il layout dei report HID. Revisione di riferimento: `fbbe58d55636ee9b81b71f9aaebba3fd8956a105`. Dettagli tecnici: [riferimento del protocollo](docs/protocol-reference.md).
- **Attribuzione MIT originale** — `Copyright (c) 2024 the_al`. L'avviso di copyright originale e il testo integrale della licenza MIT sono conservati in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
- **Microsoft .NET e Windows Forms** — Componenti runtime inclusi nel pacchetto Windows; licenze e attribuzioni sono nella sua cartella `licenses`.

## Compilare i sorgenti

Apri `DualSenseDriftLab.sln` con Visual Studio compatibile con .NET 10 oppure usa l'SDK .NET 10:

```powershell
dotnet build tests/DriftLab.Tests -c Release -m:1
dotnet run --project tests/DriftLab.Tests -c Release --no-build
dotnet publish src/DriftLab.Desktop -c Release -r win-x64 --self-contained true -m:1 -o dist/win-x64
```

Il protocollo è riferito a dualshock-tools, revisione `fbbe58d55636ee9b81b71f9aaebba3fd8956a105`. Vedi `THIRD_PARTY_NOTICES.md`, `docs/protocol-reference.md` e `docs/verification.md`. Bluetooth, DualSense Edge e altri joypad non sono supportati per la calibrazione.
