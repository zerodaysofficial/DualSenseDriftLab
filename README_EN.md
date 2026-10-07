# DualSense Drift Lab — Windows x64 — beta 3

Made by **zer0day**. DualSense calibration commands and report layouts reference **dualshock-tools**, by **the_al**, under the MIT License. The full attribution and license are included in `THIRD_PARTY_NOTICES.md`.

A native Windows application for a standard Sony DualSense connected through a USB data cable. It reads real reports, guides a left-stick return test, analyzes all four axes and attempts temporary calibration. It supports saving to the controller, restoring a backup and rebooting. It uses neither a virtual controller nor a display-only correction filter.

**Status:** compiled for Windows x64, with software tests using synthetic data and controlled transport. This version has not been run interactively on Windows or tested on a physical DualSense in this environment. USB behavior, UI layout, firmware compatibility, persistence and PS5 behavior require the checks in `hardware-checklist.md`. Calibration can correct a center offset; it cannot repair a worn sensor or guarantee that every intermittent fault disappears.

## Start

1. Close the previous version and extract the **entire** new Windows package into a new folder. Keep the DLLs and other files next to the executable.
2. Open `DualSenseDriftLab.exe`. The package includes .NET; no separate runtime installation is needed.
3. The opening screen says **Select your language** in English. Choose **English** or **Italiano**, then click **Continue**. English is selected initially. This screen appears at every launch, before USB detection starts. Closing it exits the application.
4. Connect a standard DualSense using a USB data cable. Close other applications using it. Do not change its driver.
5. Click **Test and automatic correction**. Initially leave both sticks untouched for 10 seconds. Move the left stick down and release it three times, then release it from up, right and left. Each return is observed for 8 seconds.
6. A stable left-center offset triggers a temporary left-center adjustment. Tremor, spikes, instability or a right-stick problem can trigger one confirmed temporary center calibration of **both** sticks.
7. Repeat the full test after calibration, rotate both sticks to their outer edges and release them to center. Only a complete passing verification enables **Save to controller**.

Instructions, results, app-owned errors, confirmations and Yes/No buttons use the selected language. Windows system dialogs and operating-system errors may follow the Windows language. **Made by zer0day** and the protocol attribution appear on the opening screen, in the main window and under **Credits**.

## Save, Restore and reboot

Testing and automatic adjustment do not save permanently. **Save to controller** asks for separate confirmation: keep the USB cable and internal battery connected during writing. Closing and cancellation are blocked during permanent writing. Success requires observing the old HID interface disappear, reconnecting the same serial number and reading identical calibration parameters with memory locked. A missed or very brief reboot remains unverified.

**Restore** applies a selected historical backup temporarily and verifies readback. **Save Restore** can persist that explicitly selected backup even if it contains old drift. It is not a universal factory reset.

**Reboot controller** discards temporary changes and looks for the same device for up to 15 seconds. A disconnection invalidates the test. A save error can leave the outcome uncertain; the application does not claim successful persistence without verification.

Backups remain in `%LOCALAPPDATA%\DualSenseDriftLab\Backups`. The language selection does not change existing backup identifiers, JSON fields, integrity hashes or calibration values. Backups contain 12 calibration parameters, identity, firmware, date and SHA-256 integrity information; they are not firmware images. Import rejects a different serial or firmware, an incorrect hash, unexpected or duplicate JSON fields and invalid values.

## Precision diagnostics

The application displays center X/Y, drift over time, P5–P95 oscillation width, maximum distance from the median and spike-sample counts before and after. Counts are per axis sample, not distinct spike events or tremor frequency.

These are project thresholds, not Sony specifications: accepted center about ±0.78% per axis; left-center feedback adjustment targets a reported residual of about ±0.39%; progressive drift above about 0.78%; microtremor above one USB step, about 0.78%; significant noise above 2.5%; spikes more than two steps, about 1.57%, from the median; release-dependent center differences above about 1.57%. A detected spike prevents a normal result even when percentiles hide it.

USB axes have 256 values, about 0.78% per step. The center lies between two codes at ±0.39%; ordinary alternation between those centered codes is tolerated. Exact zero cannot be represented in every individual sample. Thresholds include a small numerical tolerance.

An 8-second observation requires at least 200 samples, with no gap longer than 250 ms. Range verification requires at least ±95% on all four axes and a centered return. Center adjustment searches within ±500 parameter units of the original values, with at most 12 writes per axis. Probes may temporarily increase the offset. Failed convergence or post-calibration verification restores the previous temporary parameters and disables saving. A passing observation does not exclude a later intermittent or below-threshold fault.

## Build

Use a Visual Studio version supporting .NET 10, or the .NET 10 SDK:

```powershell
dotnet build tests/DriftLab.Tests -c Release -m:1
dotnet run --project tests/DriftLab.Tests -c Release --no-build
dotnet publish src/DriftLab.Desktop -c Release -r win-x64 --self-contained true -m:1 -o dist/win-x64
```

Protocol reference revision: `fbbe58d55636ee9b81b71f9aaebba3fd8956a105` of dualshock-tools. See `docs/protocol-reference.md` and `THIRD_PARTY_NOTICES.md`. Microsoft runtime licenses are included in the Windows package's `licenses` folder. Bluetooth, DualSense Edge and other controllers are not supported for calibration.
