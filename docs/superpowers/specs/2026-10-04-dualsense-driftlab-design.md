# DualSense Drift Lab — specifica del programma Windows

Data: 4 ottobre 2026. Stato: progetto funzionale approvato in chat; questa specifica tecnica è pronta per la revisione dell'utente. Non è un programma già realizzato.

## Obiettivo concordato

L'utente possiede un DualSense standard. Collegandolo tramite cavo USB a un PC Windows, vuole riconoscimento automatico, un test guidato dello stick sinistro, rilevamento automatico del difetto, tentativo automatico di correzione, salvataggio della calibrazione, ripristino e riavvio del controller.

Il difetto da riprodurre è preciso: dopo aver portato lo stick sinistro in basso e averlo rilasciato, il segnale si sposta lentamente verso l'alto, anche se lo stick appare fisicamente al centro. Il successo è un miglioramento misurato dopo la calibrazione, conservabile nel controller. Non basta spostare un indicatore grafico o filtrare il valore nella finestra del programma.

Assunzioni: interfaccia italiana; Windows x64; controller Sony standard originale; uso successivo anche su PS5. Le letture del software possono identificare il comportamento del segnale, non dimostrare quale componente fisico sia guasto.

## Approccio scelto e alternative

App desktop in C# con Windows Forms e .NET 10, comunicazione HID USB diretta attraverso le API di Windows. Il progetto sarà apribile in Visual Studio e pubblicabile come pacchetto Windows x64 autonomo, senza richiedere Python o driver USB sostitutivi.

Una pagina WebHID sarebbe più rapida da distribuire, ma richiederebbe un browser compatibile e una prima selezione del dispositivo. Un controller virtuale con compensazione potrebbe mitigare il problema solo nei giochi su PC e introdurrebbe driver e gestione del doppio input. Queste alternative non sono incluse nella prima versione.

La comunicazione di calibrazione deriva dal progetto pubblico dualshock-tools, rispettandone licenza e attribuzione. Prima di implementare le scritture si deve esaminare il codice sorgente del protocollo e fissare una revisione specifica: nessun comando viene inventato o ricavato soltanto dal nome di un pulsante.

## Schermata e operazioni

Una finestra mostra controller selezionato, identità, firmware quando disponibile, batteria quando disponibile, stato della connessione e posizione reale di entrambi gli stick. Lo stick sinistro ha un grafico della posizione e una traccia temporale dell'asse verticale.

Operazioni principali: **Test e correzione automatica**, **Salva nel controller**, **Restore**, **Riavvia controller**, **Esporta backup**, **Importa backup**, **Annulla test**. I comandi incompatibili con lo stato corrente sono disabilitati. Il salvataggio permanente è sempre un'azione esplicita dell'utente, separata dal tentativo automatico temporaneo.

All'apertura e all'inserimento del cavo, il programma cerca il controller. Se sono collegati più DualSense, chiede di sceglierne uno e mostra un'identità distinguibile. Se l'identità non è affidabile, permette il monitoraggio ma blocca backup ripristinabili e modifiche persistenti.

## Test del difetto

1. Acquisizione a riposo per 10 secondi, con entrambi gli stick rilasciati.
2. Indicazione di portare lo stick sinistro in basso. La posizione viene rilevata dai report, senza chiedere all'utente di stimare il valore.
3. Indicazione di rilasciare lo stick. Si rileva il rientro nella zona centrale; dopo un breve assestamento, si registra il segnale per 8 secondi.
4. Ripetizione di tre cicli. Un movimento volontario durante l'osservazione invalida quel ciclo e richiede di ripeterlo.
5. Controllo breve del rientro anche da alto, destra e sinistra, per evitare di correggere un centro che dipende dalla direzione di rilascio.

La zona centrale serve a riconoscere il rilascio, non a nascondere il difetto: analisi e grafici utilizzano sempre i dati reali. Se lo stick non rientra nella zona prevista, il programma registra il mancato rientro e non resta bloccato in attesa.

Si normalizzano gli assi nell'intervallo [-1, +1]. Ogni campione conserva il tempo monotono di ricezione. L'analisi confronta mediana iniziale e finale, variabilità robusta e scostamento rispetto al centro; distingue un errore stabile, un movimento progressivo, rumore significativo, nessun difetto rilevato o acquisizione insufficiente. Non deduce la posizione fisica dello stick dai soli report: le istruzioni richiedono che l'utente lo lasci effettivamente libero.

Soglie iniziali di progetto, da non presentare come specifiche Sony: centro accettabile entro 0,02 per asse; variazione tra prima e ultima finestra entro 0,015; ampiezza tra percentili 5 e 95 entro 0,025. Le finestre sono di un secondo. Queste soglie sono parametri documentati, non valori scelti dal programma per far risultare superato il test. Lo stesso insieme di soglie deve essere usato prima e dopo la correzione.

Per un risultato valido servono almeno 200 campioni distribuiti in ogni periodo di osservazione da 8 secondi, nessuna lacuna superiore a 250 ms e tutti i cicli richiesti. Il programma segnala insufficienza dei dati, invece di attribuirla a un guasto dello stick.

## Correzione automatica e verifica

Prima di qualunque scrittura, si leggono e si salvano su disco i parametri effettivamente ripristinabili, associati al controller. Un errore di lettura o salvataggio impedisce la correzione.

Se i campioni indicano un centro stabile ma spostato, il programma può correggere temporaneamente il centro dello stick sinistro con la funzione di regolazione del protocollo, preservando gli altri parametri. L'algoritmo deve usare la trasformazione dei parametri verificata nel codice sorgente di riferimento; non applica direttamente una mediana dei report come valore grezzo di calibrazione.

Se il segnale presenta movimento progressivo o forte variabilità, il programma segnala il limite della calibrazione. Può proporre una calibrazione temporanea guidata del centro, chiarendo che non è una riparazione dell'usura. Non tenta cicli ripetuti di correzione automatica e non modifica l'escursione degli stick per mascherare il problema.

Se la procedura disponibile coinvolge entrambi gli stick, deve essere dichiarato prima di eseguirla e l'utente deve lasciare entrambi rilasciati. Non si promette una correzione limitata a sinistra se il protocollo non la consente. Se il firmware non espone le operazioni necessarie, resta disponibile il test e viene bloccata la scrittura.

Dopo la correzione si ripete lo stesso test con nuovi campioni reali. Un esito positivo richiede che tutti i cicli rispettino le soglie del centro, della variazione progressiva e del rumore. Si controlla anche che lo stick destro non sia peggiorato e si richiede un controllo guidato della corsa completa. Senza questi controlli non si abilita il salvataggio permanente.

Esiti visibili: **Nessun difetto rilevato**, **Scostamento stabile rilevato**, **Segnale instabile: calibrazione insufficiente**, **Test dopo calibrazione superato**, **Miglioramento parziale**, **Nessun miglioramento**, **Test non valido**. Il miglioramento parziale non viene dichiarato risoluzione e non abilita il salvataggio automatico. Una calibrazione peggiorativa viene annullata temporaneamente quando il protocollo e la connessione permettono il ripristino; l'esito di quest'ultimo viene verificato e mostrato.

## Salvataggio, backup e Restore

La cronologia conserva il backup iniziale e ogni calibrazione permanente effettuata dal programma. Il backup contiene versione del formato, data UTC, identità del dispositivo, firmware, parametri grezzi ripristinabili e hash di integrità del contenuto canonico. Non è una copia completa del firmware o della memoria del controller e non viene chiamato backup di fabbrica.

Scrittura dei file tramite file temporaneo e sostituzione atomica, conservando il precedente backup valido. Destinazione sul PC dell'utente: cartella dell'app sotto LocalAppData, con possibilità di esportare una copia altrove.

Il salvataggio permanente richiede un risultato positivo recente relativo allo stesso dispositivo, backup iniziale valido, nessuna disconnessione intervenuta e stato del dispositivo compatibile. Il programma mostra l'avviso di mantenere il cavo e la batteria interna collegati durante l'operazione. Se lo stato di alimentazione non è verificabile, non descrive l'operazione come sicura: richiede il controllo dell'utente prima di procedere. La scrittura viene serializzata e, quando richiesto dal protocollo, la memoria viene nuovamente bloccata anche dopo un errore.

**Restore** ripristina una voce conservata dal programma per quel controller. Importare un backup con identità diversa, contenuto corrotto, struttura inattesa o firmware incompatibile blocca l'operazione. Non esiste un pulsante fittizio di reset universale alla calibrazione di fabbrica.

Un ripristino da cronologia viene applicato temporaneamente e verificato; l'utente può poi salvarlo permanentemente con il relativo pulsante. Riavviare o scollegare il controller scarta le modifiche temporanee, secondo le capacità effettive del protocollo. Dopo un riavvio non si dichiara il salvataggio verificato finché non si rilegge la calibrazione dalla nuova connessione.

## Riavvio ed errori

Il riavvio usa il comando del protocollo solo se verificato e supportato. Il programma chiude correttamente la sessione HID e cerca lo stesso dispositivo per un massimo di 15 secondi. Una mancata riconnessione produce un'indicazione di ricollegare il cavo, non un falso messaggio di successo. Il riavvio non è un ripristino di fabbrica.

Lettura dei report e interfaccia restano asincrone. Le scritture sono una sola alla volta. Un timeout, un report inatteso, un cavo rimosso o un'altra app che impedisce l'accesso interrompono l'operazione e invalidano i risultati non conclusi. Non si richiede la sostituzione dei driver e non si disattivano automaticamente altri programmi.

Durante una scrittura persistente non si consente di avviare altre azioni; la chiusura della finestra viene trattenuta fino al completamento o all'errore gestito. Non si garantisce il recupero da una perdita di alimentazione durante la scrittura.

## Struttura del progetto

- **Core**: campioni, test guidato, analisi, criteri di successo e macchina a stati; indipendente da Windows.
- **WindowsHid**: enumerazione e apertura dei dispositivi, lettura, cancellazione e gestione degli handle.
- **DualSenseProtocol**: decodifica USB, identificazione e operazioni di calibrazione realmente supportate.
- **Storage**: backup, integrità, compatibilità e cronologia.
- **Desktop**: Windows Forms, grafici e messaggi italiani.
- **Tests**: prove del comportamento del test e dei flussi di calibrazione con dati sintetici e trasporto controllato.

Una sola autorità gestisce la sessione controller e lo stato: scollegato, pronto, test, backup, correzione temporanea, verifica, risultato, salvataggio, ripristino, riavvio o errore. La GUI riflette questo stato e non conserva copie autonome dei permessi di scrittura.

## Verifica e consegna

Test automatici significativi: centro stabile; offset stabile; movimento progressivo dopo il rilascio; rumore; dipendenza dalla direzione; campioni insufficienti; mancato rientro; movimento dell'utente durante il test; peggioramento dopo calibrazione; calibrazione destra compromessa; backup corrotto o di altro controller; errori e disconnessioni durante le transizioni; mancata riconnessione; blocco delle scritture concorrenti.

Verifica del protocollo confrontando i report con il sorgente di riferimento fissato. I test simulati non dimostrano il comportamento di un DualSense reale. Servono poi prove USB su Windows con un controller fisico, test del salvataggio dopo riavvio e prova del ripristino prima di dichiarare queste funzioni verificate sull'hardware.

Consegna prevista: archivio del progetto Visual Studio, guida italiana e, se compilazione e verifica del pacchetto sono disponibili, pacchetto eseguibile Windows x64. La consegna distingue chiaramente sorgenti, build realmente compilata e funzioni provate su hardware; non si etichetta un archivio di codice come eseguibile.

## Fonti primarie consultate

- Protocollo e avvertenze del progetto dualshock-tools: https://github.com/dualshock-tools/dualshock-tools.github.io
- Interfaccia e spiegazioni di calibrazione, salvataggio, Restore e riavvio: https://dualshock-tools.github.io/
- Script sperimentale di calibrazione DualSense: https://github.com/dualshock-tools/ds4-tools/blob/master/ds5-calibration-tool.py
- Documentazione Microsoft .NET 10: https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview

Limite sostanziale: il progetto di riferimento indica che la calibrazione non ripara il drift dovuto a guasti o usura meccanica. Il programma automatizza la diagnosi del segnale e il tentativo di calibrazione, senza promettere una riparazione fisica.
