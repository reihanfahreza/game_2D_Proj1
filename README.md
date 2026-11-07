# Pulau Monster — M Reihan Fahreza (4352511014)

Proyek Game 2D seri 7 pertemuan (P1–P7), Program Studi Teknologi Permainan, Politeknik Negeri Batam.
RPG monster 2D tampak atas: pilih partner dari Tetua, lawan monster liar dengan battle bergiliran, naik level dan berevolusi, lalu kalahkan Raijugar di Gua Petir.

## Lingkungan
- Unity **6000.3.24f1** (JANGAN upgrade). Modul memakai 6000.6.0f1; proyek ini dibuat dengan versi yang terpasang dan semua fitur modul berjalan di versi ini.
- Template: Universal 2D, Input System (project-wide actions `InputSystem_Actions`)
- Paket tambahan: Cinemachine 3.1.7, 2D Tilemap Extras (Rule Tile)

## Alur kerja
- Branch: `NamaLengkap_NIM` → **`MReihanFahreza_4352511014`**
- Commit: `feat:` / `fix:` / `art:` / `chore:` + ringkasan
- Fetch → Update from main → kerja → tutup Unity → commit → push → PR

## Aturan kode
- Class & method: PascalCase
- Field private: camelCase + `[SerializeField]`

## Cara main

| Tombol | Fungsi |
|---|---|
| WASD / panah | Jalan |
| Shift (tahan) | Lari |
| E | Bicara dengan NPC / Batu Simpan |
| E / Spasi / Enter / Z / klik | Pilih menu, lanjut dialog |
| Esc / X / Backspace | Batal, menu jeda (Esc) |
| M | Suara hidup/mati |

Alur demo: Judul → Mulai baru → narasi → Tetua Rahmat (pilih partner, "Yakin?") → Pak Dagang (3 Ramuan) → Batu Simpan → celah pagar → rumput tinggi (monster liar) → Gua Petir → Raijugar → ending → **DEMO SELESAI** → judul (Lanjutkan).

## Isi per pertemuan

| P | Hasil | Lokasi utama |
|---|---|---|
| P1 | Repo (.gitignore, LFS, README, template PR), folder `_Project`, preset `Pre_PixelSprite`, sprite terpotong, `PlayerMovement` dengan Rigidbody2D, scene sandbox | `Scenes/Sandbox/Scn_Reihan`, `Scripts/Player` |
| P2 | `Cfg_Player` (ScriptableObject), `IMoveInput`, Composition Root `GameBootstrap` + `WorldInstaller`, `PlayFromBootstrap`, peta 40×36 (4 Tilemap, `Pal_Overworld`, `RT_Path`, `RT_Water`), tabrakan, sorting Custom Axis, Pixel Perfect | `Scenes/Scn_Bootstrap`, `Scn_World`, `Art/Tiles/Palette` |
| P3 | 8 animation clip + Blend Tree (`AC_Player`), `PlayerAnimator`, Cinemachine + Confiner 2D, `IActionInput`/`IInteractable`/`PlayerInteractor`, `Pfb_Npc` + 4 varian, `AreaZone` | `Art/Animations`, `Prefabs/NPC` |
| P4 | 12 `Mon_*` dari CSV + `MonsterDatabase`, `MonsterInstance` (level, EXP, evolusi Lv 5/9), `ElementChart`, FSM monster liar (Wander/Chase/Return), 8 monster liar + Raijugar, `GameServices`, `SceneFlow`, `BattleSession`, `InputGate`, `PartnerFollower` | `Data/Monsters`, `Scripts/Monsters`, `Scripts/World` |
| P5 | Tema UI navy-emas + Pixelify Sans, `MenuInput`, `TextMenu`, `DamageFormula`, 4 `BattleAction`, `BattleSystem` (coroutine + event), `BattleView`/`MonsterPanel`/`BattlePresenter` (MVP), HUD partner | `Scenes/Scn_Battle`, `Scripts/Battle`, `Scripts/UI` |
| P6 | `DialogueRunner` (Say/Narrate/Choose), Tetua/Penjaga/Pak Dagang (turunan `Npc`), story flag, narasi pembuka, toast, save JSON + Batu Simpan, layar judul, menu jeda, `AudioService` + musik area + mute | `Scripts/Dialogue`, `Scripts/Core/Save` |
| P7 | Gua gelap (`LightZone`, kristal, BossLight), `PixelBurst` + `GrassRustle`, `BattleFeel` (tebasan, guncang, hit-stop), ending + `Scn_End`, build .exe | `Scripts/Fx`, `Scenes/Scn_End` |

Tugas tambahan yang ikut dikerjakan: sprint Shift, 4 tembok + 3 rintangan + NPC diam (P1); `DisabledMoveInput` dan 4 sudut dalam Rule Tile (P2); `AOC_Npc_*`, clip idle 12 monster, animasi lari 1,5× (P3); monster liar di Danau Sunyi, `FleeState`, berkedip saat waktu aman (P4); angka damage melayang dan bintang saat jurus sangat efektif (P5); dialog Sari berubah setelah `boss_defeated` (P6); kilau saat naik level (P7).

## Struktur
```
Assets/_Project/
  Art/        Backgrounds, Characters, Fonts, Monsters, Tiles(+Palette), UI, VFX, Animations
  Audio/      BGM, SFX
  Data/       MonsterDatabase.csv, Mon_*.asset, MonsterDatabase, Cfg_Player, Dialogue
  Editor/     PlayFromBootstrap, DemoBuilder
  Prefabs/    NPC, Monsters, Fx
  Scenes/     Scn_Bootstrap, Scn_Title, Scn_World, Scn_Battle, Scn_End, Sandbox/Scn_Reihan
  Scripts/    Core (Input, Bootstrap, Fsm, Save, Audio, Story), Player, World, Monsters, Battle, UI, Dialogue, Fx, Dev
  Settings/   Presets/Pre_PixelSprite
```
Scene List build: 0 Bootstrap · 1 Title · 2 World · 3 Battle · 4 End. Tombol Play selalu mulai dari `Scn_Bootstrap`.

## Build
- Menu **Pulau Monster → Build Windows (.exe)** → `Builds/PulauMonster_Win/PulauMonster.exe` (Windowed 1280×720, versi 1.0.0).
- Menu **Pulau Monster → Rakit Ulang Semua** membuat ulang tile, animasi, data monster, prefab, dan semua scene dari aset Starter Asset Pack (`DemoBuilder`).

## Alat uji
- `PulauMonster.exe -autopilot` memainkan alur demo secara otomatis lalu menyimpan screenshot dan `report.txt` di `AppData/LocalLow/Politeknik Negeri Batam/Pulau Monster/autopilot`.
- Di Editor/Development Build, saat battle: F1 = HP lawan tinggal 1, F2 = HP partner tinggal 1.

## Kredit aset
- Starter Asset Pack mata kuliah (sprite, tile, UI, VFX, audio, `MonsterDatabase.csv`).
- Font Pixelify Sans — SIL Open Font License 1.1 (`Art/Fonts/PixelifySans_OFL.txt`).
