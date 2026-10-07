# Verifica della versione beta 3 — 7 ottobre 2026

78/78 test software superati in configurazione Release. Build portable: 0 avvisi, 0 errori. Include test del ciclo completo di acquisizione a sei rilasci, correzione con feedback in entrambi i versi, disconnessioni, generazioni, backup, NVS lock dopo errore, scadenza della conferma, Restore salvato, annullamento durante scrittura, readback errato e mancanza di prova del riavvio.

La revisione indipendente della precedente ricostruzione ha trovato tre problemi importanti, corretti in un unico passaggio e coperti da regressioni osservate prima fallire e poi passare. La fixture HID è stata corretta per distinguere il report firmware dal precedente comando: le regressioni su scadenza/riavvio sono state fatte fallire nuovamente con la fixture corretta.

Il salvataggio richiede nuova calibrazione verificata oppure Restore esplicitamente selezionato, stesso seriale/firmware, backup su disco, dati freschi e conferma dell'alimentazione. L'età della prova è controllata con tempo monotono anche subito prima della scrittura. Il riavvio deve produrre scomparsa dell'interfaccia nella enumerazione SetupAPI; aprire un nuovo handle non basta.

Queste prove usano dati sintetici e un trasporto controllato. Nessuna prova Windows interattiva, USB fisica, persistenza su firmware reale o PS5 è stata eseguita. Vedi hardware-checklist.md.

Rilievo minore rinviato: la percentuale/stato della batteria non è mostrata nella finestra, pur essendo decodificata e utilizzata per bloccare gli errori di alimentazione. La conferma manuale non garantisce l'assenza di guasti elettrici.

Tutte le decisioni e i costi in caso di errore sono riportati nel recovery-ledger.md. La build effettiva, i framework inclusi e il contenuto degli ZIP sono controllati separatamente; compilazione non equivale a prova sul controller.

Beta 2: le regressioni di precisione hanno mostrato il mancato rilevamento con le soglie precedenti, poi sono passate con le nuove. Sono coperti anche un picco isolato di tre codici USB, quantizzazione normale 127/128, correzione con feedback quantizzato nei due versi, statistiche aggregate dei picchi, corsa con centro fuori nuova soglia, conferma per problemi al destro e mancato salvataggio con tremolio persistente. Non sono misure di sensibilità o efficacia su un sensore fisico.

La revisione di precisione ha trovato due casi importanti: picchi su entrambe le code oltre il 5% totale, nascosti dai percentili, e una stima di pendenza che falliva sullo stesso codice USB. Le regressioni sono state osservate fallire e poi passare. Ogni picco rilevato ora impedisce la diagnosi normale; la ricerca del centro usa un intervallo limitato e tollera codici invariati. Sono verificate fasi diverse della quantizzazione, entrambi i versi, bassa sensibilità e centri vicini ai limiti ushort. È stata mantenuta la misura numerica della deriva nell’interfaccia.

Beta 3: inglese iniziale e scelta English/Italiano prima della creazione della finestra USB. Catalogo condiviso di 149 testi del programma, inclusi istruzioni dinamiche e avvisi di alimentazione, Restore e calibrazione di entrambi gli stick. Le conferme Sì/No sono controlli del programma nella lingua selezionata; i messaggi nativi Windows possono restare nella lingua del sistema. Nessuna selezione viene applicata durante una scrittura.

Tre regressioni inglesi hanno prima fallito con i testi italiani e poi passato. Il percorso italiano, i limiti dichiarati nelle conferme e la compatibilità hash/JSON dei backup attraverso il cambio di lingua sono verificati. 78/78 test Release passati, build soluzione con 0 avvisi e 0 errori. Controllo del catalogo: nessuna chiave usata mancante e nessun testo catalogato lasciato senza traduzione.

Crediti presenti nella schermata iniziale, nel footer e nella finestra Crediti: Made by zer0day; protocol reference dualshock-tools · the_al. La licenza MIT originale resta inclusa integralmente. Layout, selezione radio, dialoghi e apertura del link richiedono prova interattiva Windows; nessun screenshot di un'app diversa viene presentato come verifica.

Revisione indipendente della beta 3: 78/78 test ripetuti dal revisore, nessun rilievo Critical/Important/Minor. Controllati ordine avvio/HID, corrispondenza dei segnaposto nelle 149 traduzioni, scope degli avvisi, crediti/MIT e compatibilità dei backup. Verifica visiva Windows/DPI/focus e comportamento fisico rimangono da eseguire; contenuto finale degli archivi controllato dall’autore.
