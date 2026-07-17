# SightAdapt Light

## 1. Cel

Wersja Light jest minimalną, samodzielną aplikacją, której zadaniem jest zmiana kolorystyki obrazu jednego lub kilku okien bez ingerowania w proces źródłowy.

Podstawowy scenariusz:

1. użytkownik aktywuje okno dowolnej aplikacji;
2. naciska `Ctrl+Win+2`;
3. SightAdapt rozpoznaje aktywne okno;
4. uruchamia przechwytywanie tego okna;
5. stosuje aktywny profil kolorystyczny;
6. wyświetla przetworzony obraz w nakładce;
7. ponowne naciśnięcie skrótu wyłącza efekt.

## 2. Zakres wersji Light

### 2.1 Funkcje wymagane

- przechwytywanie konkretnego okna na podstawie `HWND`;
- odwrócenie kolorów;
- zastosowanie pliku LUT;
- regulacja jasności;
- regulacja kontrastu;
- regulacja nasycenia;
- skala szarości;
- szybkie włączanie i wyłączanie;
- globalny skrót klawiaturowy;
- profile per aplikacja;
- automatyczne dopasowanie nakładki do położenia okna;
- obsługa minimalizacji i zamykania aplikacji;
- obsługa wielu monitorów;
- obsługa skalowania DPI;
- działanie w tle;
- ikona w zasobniku systemowym;
- automatyczny start po zalogowaniu jako opcja;
- log diagnostyczny bez danych ekranowych;
- konfiguracja w pliku JSON;
- import i eksport profili.

### 2.2 Funkcje poza zakresem

W wersji Light nie będą implementowane:

- OCR;
- synteza mowy;
- modyfikacja czcionek w obcej aplikacji;
- zmiana układu kontrolek;
- automatyczne klikanie;
- przekazywanie przeskalowanych współrzędnych myszy;
- DLL injection;
- globalne hooki myszy;
- sterownik systemowy;
- nagrywanie obrazu;
- zdalne przesyłanie obrazu;
- rozbudowany edytor dostępności;
- obsługa semantyczna UI Automation poza identyfikacją okna.

## 3. Platformy

### 3.1 System bazowy

Pierwszą platformą testową jest:

- Windows 10 22H2 x64.

### 3.2 Zgodność z Windows 11

Kod musi być projektowany tak, aby:

- nie używał nieudokumentowanych struktur systemowych;
- nie zakładał stałych rozmiarów ramek okna;
- korzystał z per-monitor DPI awareness;
- używał oficjalnych interfejsów Win32, WinRT i DirectX;
- pozwalał wykrywać dostępność nowszych funkcji w runtime;
- nie uzależniał działania od konkretnego wyglądu paska zadań;
- nie opierał się na numerze wersji systemu, jeśli można wykryć konkretną funkcję.

Minimalna wersja systemu jest sprawdzana przy uruchomieniu.

## 4. Decyzje technologiczne

### 4.1 Język i runtime

- C#;
- .NET 10;
- kompilacja x64;
- publikacja self-contained jako podstawowy wariant wydania.

Nie przewiduje się kompilacji x86.

### 4.2 Interfejs użytkownika

- WPF dla panelu ustawień;
- osobne natywne okna Win32 dla nakładek;
- ikona w zasobniku systemowym;
- brak głównego okna widocznego przez cały czas.

### 4.3 Integracja Win32

Rekomendowane użycie:

- `Microsoft.Windows.CsWin32`;
- własne, minimalne wrappery SafeHandle;
- unikanie ręcznego powielania deklaracji P/Invoke.

### 4.4 Przechwytywanie

Podstawowy backend:

- Windows Graphics Capture;
- `IGraphicsCaptureItemInterop::CreateForWindow`;
- `Direct3D11CaptureFramePool`;
- klatki pozostają w pamięci GPU.

Backend zapasowy, planowany dopiero po ustabilizowaniu głównego:

- Desktop Duplication API.

### 4.5 Renderowanie

- Direct3D 11;
- DXGI;
- HLSL;
- Vortice.Windows jako binding .NET;
- jeden pełnoekranowy prostokąt renderowany do nakładki;
- efekty realizowane shaderem pikselowym;
- brak kopiowania całej klatki do tablic zarządzanych.

### 4.6 Nakładka

Nakładka jest osobnym oknem:

- bez ramki;
- bez aktywacji;
- bez wpisu w `Alt+Tab`;
- bez przyjmowania fokusu;
- przepuszczającym mysz;
- zsynchronizowanym z oknem źródłowym;
- ukrywanym przy minimalizacji;
- zamykanym po utracie prawidłowego `HWND`.

Domyślnie nakładka ma skalę 1:1.

## 5. Architektura

```text
Global Hotkey
     │
     ▼
Active Window Resolver
     │
     ▼
Profile Matcher
     │
     ▼
Overlay Session Manager
     │
     ├── Window Tracker
     ├── Capture Session
     ├── Effect Pipeline
     └── Overlay Window
```

### 5.1 Moduły

#### `SightAdapt.App`

Odpowiada za:

- uruchomienie procesu;
- tray;
- okno ustawień;
- globalne skróty;
- autostart;
- komunikaty dla użytkownika.

#### `SightAdapt.Core`

Odpowiada za:

- modele profili;
- walidację konfiguracji;
- dopasowanie profilu;
- stan sesji;
- logikę niezależną od Windows.

#### `SightAdapt.Platform.Win32`

Odpowiada za:

- enumerację okien;
- pobranie aktywnego `HWND`;
- identyfikację procesu;
- śledzenie zdarzeń okna;
- DPI;
- pozycję i widoczne granice;
- zarządzanie stylami nakładki;
- globalne hotkeye.

#### `SightAdapt.Capture.Wgc`

Odpowiada za:

- tworzenie `GraphicsCaptureItem`;
- frame pool;
- odbiór nowych klatek;
- zmianę rozmiaru źródła;
- zatrzymanie sesji;
- obsługę utraty urządzenia.

#### `SightAdapt.Rendering.D3D11`

Odpowiada za:

- urządzenie Direct3D;
- swapchain;
- kompilację shaderów;
- bufor parametrów efektu;
- LUT;
- prezentację klatki;
- pomiary czasu GPU.

#### `SightAdapt.Overlays`

Odpowiada za:

- utworzenie okna nakładki;
- synchronizację kolejności Z;
- ukrywanie i pokazywanie;
- bezpieczne zamykanie;
- zabezpieczenie przed przechwytywaniem własnego okna.

## 6. Śledzenie okna

Podstawowym mechanizmem są zdarzenia systemowe:

- `SetWinEventHook`;
- `EVENT_SYSTEM_FOREGROUND`;
- `EVENT_OBJECT_LOCATIONCHANGE`;
- `EVENT_OBJECT_SHOW`;
- `EVENT_OBJECT_HIDE`;
- `EVENT_OBJECT_DESTROY`;
- zdarzenia minimalizacji.

Dodatkowy timer kontrolny może działać z niską częstotliwością, na przykład raz na sekundę, wyłącznie w celu wykrycia utraconych zdarzeń.

Nie należy przesuwać nakładki w pętli o wysokiej częstotliwości bez potrzeby.

### 6.1 Granice okna

Preferowana kolejność:

1. `DwmGetWindowAttribute` z `DWMWA_EXTENDED_FRAME_BOUNDS`;
2. fallback do `GetWindowRect`;
3. korekcja zgodnie z DPI monitora.

## 7. DPI

Proces musi być per-monitor DPI aware.

Wymagania:

- deklaracja DPI awareness w manifeście;
- współrzędne przechowywane w pikselach fizycznych;
- reagowanie na zmianę monitora;
- testowanie 100%, 125%, 150%, 175%, 200%;
- brak założenia, że wszystkie monitory mają identyczne DPI.

## 8. Pipeline efektów

Rekomendowana kolejność:

```text
Źródłowa tekstura
    ↓
Konwersja przestrzeni / normalizacja
    ↓
Macierz kolorów
    ↓
Jasność i kontrast
    ↓
Gamma
    ↓
LUT
    ↓
Opcjonalne wyostrzenie
    ↓
Wyjście
```

W pierwszym publicznym MVP wystarczy:

- invert;
- grayscale;
- brightness;
- contrast;
- saturation;
- LUT.

### 8.1 Odwrócenie kolorów

Podstawowe odwrócenie:

```hlsl
rgb = 1.0 - rgb;
```

Alfa nie powinna być odwracana.

### 8.2 LUT

Wspierane formaty początkowe:

- `.cube` 3D LUT;
- opcjonalnie prosty wewnętrzny format JSON dla macierzy i krzywych.

Wymagania:

- walidacja rozmiaru;
- limit wielkości;
- czytelny komunikat o błędzie;
- brak wykonywania kodu z pliku profilu;
- cache tekstur LUT.

## 9. Model profilu

```json
{
  "schemaVersion": 1,
  "id": "accounting-high-contrast",
  "name": "Księgowość — wysoki kontrast",
  "enabled": true,
  "match": {
    "executablePath": "C:\\Program Files\\Vendor\\App.exe",
    "processName": "App",
    "windowClass": null,
    "titleRegex": null
  },
  "scope": "activeWindow",
  "effects": {
    "invert": true,
    "grayscale": 0.0,
    "brightness": 0.0,
    "contrast": 1.2,
    "saturation": 1.0,
    "gamma": 1.0,
    "lutPath": null
  },
  "hotkey": null
}
```

### 9.1 Dopasowanie

Priorytet:

1. pełna ścieżka pliku wykonywalnego;
2. nazwa procesu;
3. klasa okna;
4. tytuł okna.

Wyrażenia regularne tytułów muszą mieć timeout.

## 10. Zarządzanie sesją

Jedna sesja odpowiada jednemu oknu źródłowemu.

Stan sesji:

```text
Created
  ↓
Starting
  ↓
Running
  ↓
Suspended
  ↓
Stopping
  ↓
Disposed
```

Możliwe stany błędów:

- `TargetWindowClosed`;
- `CaptureUnavailable`;
- `DeviceLost`;
- `OverlayCreationFailed`;
- `ProtectedContent`;
- `PermissionDenied`;
- `UnsupportedSystem`.

Każdy błąd musi prowadzić do ukrycia lub zamknięcia nakładki.

## 11. Bezpieczeństwo użytkownika

### 11.1 Skrót awaryjny

`Ctrl+Win+Shift+2`:

- zamyka wszystkie nakładki;
- zatrzymuje wszystkie sesje;
- działa nawet wtedy, gdy panel ustawień nie odpowiada;
- nie wykonuje zapisu ustawień przed wyłączeniem efektu.

### 11.2 Watchdog wewnętrzny

Aplikacja powinna:

- wykrywać brak prezentacji kolejnych klatek;
- po określonym czasie ukryć zamrożoną nakładkę;
- odtworzyć urządzenie Direct3D po utracie urządzenia;
- nigdy nie pozostawić nieaktualnego obrazu blokującego widok aplikacji.

### 11.3 Brak ingerencji

Zakazane w wersji Light:

- `SetWindowsHookEx` dla globalnej myszy;
- wstrzykiwanie biblioteki;
- modyfikowanie pamięci procesu;
- wysyłanie nieudokumentowanych komunikatów;
- zmiana stylów okna źródłowego;
- uruchamianie jako administrator bez wyraźnej potrzeby.

## 12. Prywatność

- klatki nie są zapisywane;
- klatki nie opuszczają GPU, o ile nie wymaga tego diagnostyka deweloperska;
- brak funkcji zrzutu ekranu;
- brak uploadu;
- brak telemetryki opt-out;
- log zawiera tylko dane techniczne, np. kod błędu, typ GPU, wersję programu;
- ścieżki aplikacji mogą być maskowane w raportach błędów.

## 13. Działanie w tle

- zwykły proces użytkownika;
- ikona w trayu;
- opcjonalny autostart;
- brak usługi Windows;
- brak wymogu uprawnień administratora;
- profil uruchamiany tylko dla dopasowanego okna;
- renderowanie zatrzymywane po ukryciu lub minimalizacji celu.

## 14. Wydajność

Cele dla jednej nakładki 1920×1080:

- średnie użycie CPU poniżej 5% na typowym komputerze biurowym;
- przetwarzanie w całości na GPU;
- brak alokacji zarządzanych na każdą klatkę;
- brak kopiowania tekstury do CPU;
- narzut opóźnienia docelowo poniżej jednej klatki;
- automatyczne ograniczenie liczby klatek dla statycznego obrazu, jeśli backend na to pozwala.

Pomiary należy wykonywać za pomocą:

- PIX;
- Windows Performance Recorder;
- Windows Performance Analyzer;
- liczników własnych aplikacji.

## 15. Testy

### 15.1 Aplikacje testowe

Minimalny zestaw:

- Notatnik;
- Eksplorator plików;
- klasyczna aplikacja Win32;
- WinForms;
- WPF;
- przeglądarka Chromium;
- aplikacja Electron;
- Qt;
- aplikacja z renderowaniem DirectX;
- aplikacja uruchomiona jako administrator.

### 15.2 Scenariusze

- szybkie włączanie i wyłączanie;
- zamykanie źródłowego okna;
- minimalizowanie;
- maksymalizowanie;
- przyciąganie do krawędzi;
- przechodzenie między monitorami;
- zmiana DPI;
- blokowanie ekranu;
- uśpienie i wznowienie;
- restart sterownika GPU;
- podłączanie monitora;
- rozłączanie monitora;
- zmiana orientacji;
- praca 8 godzin;
- 100 kolejnych aktywacji i dezaktywacji.

## 16. Kryteria ukończenia Light

Wersja Light jest ukończona, gdy:

1. odwracanie kolorów działa stabilnie dla aplikacji testowych;
2. LUT działa bez kopiowania obrazu do CPU;
3. nakładka nie przechwytuje myszy ani klawiatury;
4. skrót awaryjny działa w każdej normalnej sytuacji;
5. nie występują znane przypadki pozostawienia zamrożonego obrazu po błędzie;
6. przesuwanie i zmiana rozmiaru są wystarczająco płynne;
7. aplikacja działa na Windows 10 22H2 oraz Windows 11;
8. nie występuje narastający wyciek pamięci w teście ośmiogodzinnym;
9. ustawienia są wersjonowane i migrowalne;
10. istnieją testy automatyczne dla logiki profili;
11. publikowane wydanie zawiera sumy kontrolne;
12. repozytorium zawiera dokumentację kompilacji i zgłaszania błędów;
13. publiczne wydanie zostało przetestowane z udziałem osób niedowidzących;
14. lista znanych ograniczeń jest jawna.

## 17. Elementy przygotowujące wersję Hard

Już w Light należy zachować:

- interfejs `IWindowFrameSource`;
- interfejs `IEffectPipeline`;
- interfejs `IOverlaySession`;
- wersjonowany model profilu;
- możliwość dodawania efektów bez zmiany capture backendu;
- rozdzielenie logiki UI od renderera;
- obsługę wielu sesji, nawet jeśli interfejs MVP eksponuje tylko jedną;
- identyfikator okna źródłowego i procesu;
- kanał zdarzeń dla fokusu oraz położenia;
- architekturę modułową bez zależności `Core` od WPF.

Nie należy jednak implementować funkcji Hard przed ukończeniem Light.
