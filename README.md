# SightAdapt

Open-source'owa aplikacja dostępnościowa dla Windows 10 i Windows 11, umożliwiająca zmianę sposobu wyświetlania wybranych okien aplikacji, nawet gdy same aplikacje nie oferują odpowiednich ustawień.

Projekt jest rozwijany etapami:

1. **Light** — szybka, stabilna nakładka odwracająca kolory lub stosująca LUT do wskazanego okna.
2. **Hard** — rozszerzona powłoka dostępnościowa, budowana dopiero po osiągnięciu pełnej stabilności wersji Light.

## Cel projektu

Celem jest umożliwienie osobom niedowidzącym korzystania z aplikacji, które:

- nie mają trybu wysokiego kontrastu;
- nie obsługują motywów dostępnościowych;
- używają niewłaściwych kombinacji kolorów;
- nie pozwalają na indywidualną korekcję obrazu;
- nie udostępniają odpowiednich opcji powiększania i wyróżniania elementów.

Aplikacja nie modyfikuje plików ani pamięci obcych programów. Wyświetla przetworzony obraz ich okien w osobnej, przezroczystej dla wejścia nakładce.

## Dokumentacja

- [LIGHT.md](LIGHT.md) — zakres, architektura i kryteria ukończenia wersji Light.
- [HARD.md](HARD.md) — docelowa architektura rozszerzonej powłoki dostępnościowej.

## Główne założenia

- podstawowy język: **C#**;
- środowisko uruchomieniowe: **.NET 10 x64**;
- interfejs ustawień: **WPF**;
- integracja systemowa: **Win32**;
- przechwytywanie okna: **Windows Graphics Capture**;
- renderowanie: **Direct3D 11 + HLSL**;
- nakładka: natywne okno Win32 i powierzchnia GPU;
- zgodność początkowa: **Windows 10 22H2**;
- zgodność rozwijana równolegle: **Windows 11**;
- bez DLL injection;
- bez sterowników;
- bez przechowywania obrazu ekranu;
- bez obowiązkowej telemetrii;
- pełna publikacja kodu źródłowego na GitHubie.

## Priorytety

Kolejność priorytetów projektu:

1. bezpieczeństwo użytkownika;
2. możliwość natychmiastowego wyłączenia efektu;
3. stabilność nakładki;
4. brak wpływu na działanie przetwarzanej aplikacji;
5. małe opóźnienie;
6. niski narzut CPU i GPU;
7. przewidywalność działania;
8. dopiero później liczba funkcji.

## Domyślne sterowanie

| Skrót | Działanie |
|---|---|
| `Ctrl+Win+2` | Włącz lub wyłącz profil dla aktywnego okna |
| `Ctrl+Win+Shift+2` | Awaryjnie wyłącz wszystkie nakładki |
| `Ctrl+Win+3` | Przełącz na następny profil kolorystyczny |
| `Ctrl+Win+0` | Otwórz panel ustawień |

Skróty muszą być konfigurowalne.

## Model rozwoju

### Faza A — Light

Wersja Light musi zapewnić:

- przechwycenie wybranego okna;
- odwrócenie kolorów;
- zastosowanie LUT;
- działanie globalnego skrótu;
- automatyczne śledzenie położenia okna;
- pełne przepuszczanie myszy i klawiatury;
- brak migotania;
- obsługę wielu monitorów;
- zgodność z różnymi skalami DPI;
- stabilne działanie przez wiele godzin.

Rozwój funkcjonalny wersji Hard nie rozpoczyna się przed osiągnięciem kryteriów stabilności opisanych w `LIGHT.md`.

### Faza B — Hard

Wersja Hard dodaje warstwę semantyczną i narzędzia dostępnościowe:

- zarządzanie wieloma aktywnymi nakładkami;
- podświetlenie fokusu;
- lupę;
- panel powiększonego tekstu;
- integrację z UI Automation;
- profile per aplikacja;
- opcjonalny OCR;
- opcjonalną syntezę mowy;
- reguły obsługi dialogów i okien potomnych.

## Proponowana struktura repozytorium

```text
SightAdapt/
├── README.md
├── LICENSE
├── SECURITY.md
├── CONTRIBUTING.md
├── docs/
│   ├── LIGHT.md
│   ├── HARD.md
│   ├── architecture/
│   └── adr/
├── src/
│   ├── SightAdapt.App/
│   ├── SightAdapt.Core/
│   ├── SightAdapt.Platform.Win32/
│   ├── SightAdapt.Capture.Wgc/
│   ├── SightAdapt.Rendering.D3D11/
│   ├── SightAdapt.Overlays/
│   └── SightAdapt.Accessibility/
├── tests/
│   ├── SightAdapt.UnitTests/
│   ├── SightAdapt.IntegrationTests/
│   └── TestApplications/
└── tools/
```

## Licencja

Rekomendowana licencja: **MIT**.

Pozwala ona na:

- użycie prywatne i komercyjne;
- modyfikowanie kodu;
- redystrybucję;
- tworzenie forków;
- integrację z innymi projektami dostępnościowymi.

Projekt powinien dodatkowo zawierać:

- `SECURITY.md`;
- politykę prywatności;
- opis zagrożeń;
- informację, że nakładka nie gwarantuje działania z treściami chronionymi DRM;
- informację o ograniczeniach dla aplikacji uruchomionych z wyższymi uprawnieniami.

## Definicja ukończenia projektu Light

Wersję Light uznaje się za gotową do publicznej publikacji dopiero wtedy, gdy:

- nie wymaga uprawnień administratora do zwykłego działania;
- działa co najmniej na Windows 10 22H2 i aktualnym Windows 11;
- użytkownik może zawsze wyłączyć nakładkę skrótem awaryjnym;
- awaria renderera nie blokuje pulpitu;
- nakładka nie przechwytuje wejścia;
- przetwarzanie nie wykonuje kopiowania każdej klatki do pamięci CPU;
- aplikacja poprawnie reaguje na minimalizowanie, zamykanie i przenoszenie okien;
- test długotrwały trwa co najmniej 8 godzin bez narastania zużycia pamięci;
- nie występuje rekurencyjne przechwytywanie własnej nakładki;
- profil można przypisać do pliku wykonywalnego aplikacji;
- ustawienia można wyeksportować i zaimportować.

## Status dokumentu

Dokument określa założenia startowe. Zmiany architektoniczne powinny być zapisywane jako ADR w katalogu `docs/adr`.
