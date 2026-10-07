Recupero 5 ottobre 2026

La manutenzione ha rimosso la precedente implementazione (8d9ac0e), test e build; solo spec e piano erano stati salvati. Nessuna prova del vecchio codice vale per questa ricostruzione.

Ruling: ricostruire da spec approvata e riepilogo tecnico, ripetendo tutte le prove — il codice perso non è recuperabile — costo: nuova implementazione da verificare.
Ruling: build MSBuild -m:1 — vincolo ambiente già osservato — costo: compilazione meno parallela.
Ruling: la perturbazione temporanea di 32 raw può brevemente aumentare offset — misura necessaria per il verso del feedback — costo: breve offset temporaneo.
Ruling: Restore verificato con identità+firmware+hash+readback può essere salvato esplicitamente senza test positivo — permette tornare al backup originale anche col vecchio drift — costo: può ripristinare il vecchio difetto.

Final: fixed live guided acquisition overflow — portable SampleAcquisition complete six-cycle trace RED (OverflowException) → GREEN.
Final: fixed stale proof across user confirmation — controllable monotonic clock and expiry-before-commit test RED (Expected rejection) → GREEN; wall-clock-independent elapsed time.
Final: fixed false confirmation without observed reset — explicit Reconnection.ResetObserved evidence and missing-reset test RED (Expected rejection) → GREEN; native disappearance uses raw SetupAPI presence, not permission to open a HID handle.
Final: fixture corrected — feature 0x20 must not inherit preceding 0x81 command payload; happy Restore/save test caught the false-negative fixture before expiry/reboot regressions were verified again.
Final: suite 55/55 Release passed, 0 build warnings and errors in portable projects.
Final: minor (deferred): battery percentage/status is decoded and gates battery error, but is not displayed in the GUI; manual power confirmation still required.
Final: Ruling: native HID behavior and GUI rendering outside reviewer environment — deliver explicitly hardware-untested beta with checklist — cost: firmware/driver/layout incompatibility may remain.
Final: Ruling: PS5 and mechanical improvement require physical testing — no guaranteed repair claim — cost: changing/worn sensor drift may persist.
Final: Ruling: final documents and archives were outside immutable code review — inspect license, hashes, PE, included runtime and archives in packaging step — cost: packaging inspection is by the author.
Final: Ruling: validated historical Restore persistence accepted by reviewer — preserve explicit Save Restore exception — cost: old drift may return (same decision as prior Restore ruling).
Final: Ruling: require observable HID disappearance before accepting reboot — avoid affirming persistence from temporary RAM alone — cost: an extremely brief reset may be missed and remain unverified.
Final: Ruling: deliver archives while retaining standalone feature branch — no shared repository, remote publication or merge requested — cost: no published Git repository.

Beta 2: Ruling: thresholds tightened using eight-bit USB quantization — catch small offsets and two-code oscillation while tolerating centered 127/128 alternation — cost: stricter verification may reject a previously accepted controller; project thresholds, not Sony specifications.
Beta 2: Ruling: classify all four axes and require confirmation for a right-stick problem — left-only automatic correction cannot fix the right center — cost: both-stick temporary calibration needs user participation.
Beta 2: Ruling: report peak and sample count, with no filtered output or perfect-repair claim — intermittent hardware noise cannot be guaranteed cured by a center calibration — cost: unresolved defects block Save and roll back.
Beta 2: suite 69/69 Release passed; additional precision regressions observed failing before fixes. Quantized feedback tests supplement the continuous-feedback fixtures.

Beta 2 review fixes: paired-tail spikes now cannot be Normal; regression on all 4 axes and Save gate RED → GREEN. Quantized plateau regression RED → GREEN with bounded center bracket/bisection replacing one-shot slope extrapolation. Continuous and quantized response directions, low sensitivity and ushort boundary centers verified.
Beta 2: Ruling: bracket endpoints temporarily move the center up to ±500 raw parameter units, maximum 12 writes per axis — avoids false failure on an unchanged quantized code; rollback and full physical retest remain mandatory — cost: temporary offset may briefly grow while probing. The earlier 32-unit probe ruling applies to the previous beta.
Beta 2: Ruling: reviewer cannot assess physical sensitivity/PS5, Windows rendering, unchanged HID mechanics or final archives — keep hardware limits explicit and inspect package bytes locally — cost: no hardware or visual Windows validation here. Historical Restore exception remains authorized.
Beta 2: full suite 72/72 Release; thresholds and successful observed test are not a guarantee against a later intermittent defect.

Beta 3: Ruling: English language chooser at every launch with English then Italiano, no persistence preference — follows explicit user request and resolves UI text before opening HID — cost: one selection per launch.
Beta 3: Ruling: localize app-owned text and confirmation Yes/No while preserving protocol bytes, backup metadata and save protections — system dialogs/errors remain Windows-owned — cost: native dialogs may use OS language.
Beta 3: Ruling: visible Made by zer0day and dualshock-tools/the_al protocol attribution, retaining original MIT terms — app authorship is distinguished from protocol reference — cost: no claim of inventing the device protocol.
Beta 3: three English behavioral regressions observed RED → GREEN; 78/78 software tests pass including Italian instructions, backup hash/JSON identity across language change and both-stick/wear warning semantics. Catalog audit covers 149 keys. No interactive Windows/physical-controller validation here.

Beta 3 independent review: no blocking or minor findings; 78/78 rerun and 149-key/placeholder coverage verified. Ruling: accept beta delivery while Windows rendering/DPI/modal-focus and native system-language behavior remain outside the available environment; unchanged HID/calibration efficacy retains prior limits; author checks final package contents. Cost: user must run documented Windows smoke checks, not interpret compile/test success as physical or visual validation.
