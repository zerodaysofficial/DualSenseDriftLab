# Riferimento del protocollo

Repository: https://github.com/dualshock-tools/dualshock-tools.github.io
Revisione: fbbe58d55636ee9b81b71f9aaebba3fd8956a105

File primari recuperati e consultati (SHA-256):

- `js/controllers/ds5-controller.js`: `ff542885459a50bf4308aebd532accbd81cfe6aa55880032c3839052b96090b0`
- `js/controllers/base-controller.js`: `dd214bf4a1bdabac650c3aa39d80cf4cdd3b49492defd279619e1daf6dc64e09`
- `LICENSE.txt`: `e322e48bd550372501c0dcb650b18eabcf6337856cf3b87fabf107989a3c1df3`

Report feature letti includono ID. Gli input native HID includono ID; i campioni WebHID del riferimento lo escludono: assi agli offset 1–4 native, batteria offset 53 native. Lettura calibrazione 0x80 [12,2], risposta 0x81 [12,2|4,2], 12 ushort little-endian da offset 4. Scrittura 0x80 [12,1]+24 byte; ordine LL,LT,RL,RT,LR,LB,RR,RB,LX,LY,RX,RY. Centri sinistri 8/9.

NVS: unlock [3,2,101,50,64,12], lock [3,1], query [3,3]. La query usa un uint big-endian da offset 1: 0x03030201 locked, 0x03030200 unlocked, 0x15010100 pending reboot. Altri stati bloccano le operazioni. Reboot [1,1].

Centro entrambi gli stick: ID 0x82 begin [1,1,1], sample [3,1,1], end [2,1,1]; risposte 0x83 [1,1,1] e [1,1,2]. Nessuna calibrazione dell’escursione automatica.

Il riferimento non prova la compatibilità di ogni firmware né una riparazione di sensori usurati. La prova fisica deve confermare report e persistenza.
