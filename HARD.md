# SightAdapt Hard

## 1. Cel

Wersja Hard jest rozszerzoną powłoką dostępnościową działającą nad aplikacjami, które nie zapewniają wystarczającej dostępności.

Nie zastępuje wersji Light. Wykorzystuje jej stabilny mechanizm przechwytywania, renderowania i nakładek, a następnie dodaje:

- warstwę semantyczną;
- dodatkowe nakładki;
- analizę fokusu;
- narzędzia powiększające;
- panel tekstowy;
- bardziej rozbudowane reguły profili.

Rozwój wersji Hard zaczyna się dopiero po spełnieniu wszystkich kryteriów ukończenia Light.

## 2. Główna zasada architektoniczna

Wersja Hard nie może osłabiać podstawowych gwarancji Light:

- nakładka nie przechwytuje wejścia bez wyraźnie włączonego trybu interaktywnego;
- aplikacja źródłowa nie jest modyfikowana;
- funkcje semantyczne mogą zawieść bez wyłączania transformacji kolorów;
- każdy moduł dodatkowy może zostać osobno wyłączony;
- awaria OCR lub UI Automation nie zatrzymuje renderera;
- podstawowy skrót awaryjny zawsze wyłącza wszystkie warstwy.

## 3. Docelowe funkcje

### 3.1 Profile zaawansowane

Profil może zawierać:

- transformację kolorów;
- LUT;
- kontrast lokalny;
- wyostrzenie;
- wzmocnienie krawędzi;
- filtr redukujący olśnienie;
- przyciemnienie obszaru poza aktywnym oknem;
- linijkę do czytania;
- wyróżnienie kursora;
- wyróżnienie fokusu;
- panel tekstowy;
- ustawienia lupy;
- reguły dla okien dialogowych;
- reguły dla konkretnych kontrolek.

### 3.2 UI Automation

UI Automation jest źródłem danych semantycznych, a nie mechanizmem bezpośredniego stylowania obcych aplikacji.

Możliwe zastosowania:

- wykrywanie aktywnej kontrolki;
- pobieranie nazwy elementu;
- pobieranie typu kontrolki;
- pobieranie wartości;
- pobieranie zaznaczonego tekstu;
- pobieranie prostokąta ekranowego;
- śledzenie fokusu;
- wykrywanie pola edycji;
- określanie stanu zaznaczenia;
- budowanie panelu powiększonego tekstu;
- podświetlanie elementów;
- opcjonalna synteza mowy.

Ograniczenia:

- nie każda aplikacja ujawnia poprawne drzewo;
- aplikacje custom-rendered mogą udostępniać niewiele danych;
- aplikacje uruchomione z wyższymi uprawnieniami mogą być niedostępne;
- przeglądarki i aplikacje Electron mogą generować bardzo duże drzewa;
- odczyt musi mieć timeout;
- zapytania UIA nie mogą blokować wątku renderującego.

### 3.3 Powiększony panel tekstowy

Panel może pokazywać:

- nazwę aktywnej kontrolki;
- aktualną wartość;
- zaznaczony tekst;
- zawartość pola edycji;
- tekst z kursora;
- tekst rozpoznany przez OCR.

Panel jest renderowany przez aplikację i może mieć:

- dowolny rozmiar czcionki;
- własny krój pisma;
- wysoki kontrast;
- regulowany odstęp między literami;
- regulowany odstęp między liniami;
- zawijanie;
- wyróżnianie aktualnego słowa;
- tryb zawsze na wierzchu;
- tryb przypięcia do monitora.

### 3.4 Lupa

Preferowane tryby:

- lupa pod kursorem;
- lupa aktywnej kontrolki;
- lupa przypięta;
- lupa pełnoekranowa jako osobny tryb;
- lupa pokazująca tylko wskazany fragment źródłowego okna.

W początkowej wersji Hard lupa jest nieinteraktywna. Użytkownik klika w oryginalną aplikację.

Interaktywne mapowanie współrzędnych może zostać rozważone później jako moduł eksperymentalny.

### 3.5 Podświetlenie fokusu

Nakładka może rysować:

- grubą ramkę;
- półprzezroczyste tło;
- wskaźnik kierunku;
- numer kolejności;
- etykietę z nazwą elementu.

Źródło położenia:

1. UI Automation;
2. zdarzenia dostępności WinEvent;
3. OCR lub analiza obrazu jako ostateczny fallback.

### 3.6 Kursor tekstowy

Możliwe mechanizmy:

- UI Automation TextPattern;
- zdarzenia systemowe;
- Accessibility API aplikacji;
- analiza obrazu jako fallback.

Funkcja musi tolerować brak danych.

### 3.7 Linijka do czytania

Tryby:

- poziomy pas pod kursorem;
- poziomy pas pod fokusem;
- przyciemnienie obszaru powyżej i poniżej;
- regulowana wysokość;
- regulowana przezroczystość;
- śledzenie wiersza tekstu, jeśli UI Automation udostępnia pozycję.

### 3.8 OCR

OCR jest modułem opcjonalnym.

Zastosowania:

- aplikacje bez UI Automation;
- tekst narysowany w canvas;
- stare programy;
- skany;
- zdalne pulpity.

Zasady:

- OCR uruchamiany lokalnie;
- analiza tylko wskazanego obszaru;
- ograniczona częstotliwość;
- cache wyniku;
- możliwość całkowitego wyłączenia;
- jawny wskaźnik aktywności;
- brak zapisywania obrazu;
- błędy OCR nie mogą wyłączać innych funkcji.

### 3.9 Synteza mowy

Opcjonalne funkcje:

- odczyt nazwy aktywnej kontrolki;
- odczyt zaznaczonego tekstu;
- odczyt wyniku OCR;
- skrót „czytaj od tego miejsca”;
- przerwanie mowy;
- regulacja głosu i szybkości.

Synteza mowy jest osobnym modułem i nie powinna działać domyślnie.

## 4. Elementy, których nie należy obiecywać

Projekt nie gwarantuje uniwersalnej możliwości:

- zmiany prawdziwego rozmiaru czcionki w obcej aplikacji;
- przeorganizowania interfejsu;
- zmiany wysokości kontrolek;
- wymuszenia reflow;
- zmiany stylu każdej kontrolki;
- poprawnej obsługi aplikacji chronionych;
- obsługi treści DRM;
- ingerencji w bezpieczny pulpit UAC;
- obsługi wszystkich gier pełnoekranowych;
- działania z aplikacjami blokującymi przechwytywanie.

Zamiast tego projekt zapewnia funkcje zewnętrzne:

- przetwarzanie obrazu;
- powiększenie;
- panel tekstowy;
- podświetlenie;
- odczyt semantyczny;
- syntezę mowy.

## 5. Rozszerzona architektura

```text
                    ┌──────────────────┐
                    │ Profile Manager  │
                    └────────┬─────────┘
                             │
                ┌────────────▼────────────┐
                │ Accessibility Orchestrator│
                └───────┬────────┬────────┘
                        │        │
          ┌─────────────▼──┐  ┌──▼──────────────┐
          │ Visual Pipeline │  │ Semantic Pipeline│
          └──────┬──────────┘  └──────┬──────────┘
                 │                    │
        ┌────────▼────────┐   ┌───────▼─────────┐
        │ Capture + D3D11 │   │ UIA / OCR / TTS │
        └────────┬────────┘   └───────┬─────────┘
                 │                    │
          ┌──────▼────────────────────▼──────┐
          │ Composite Overlay Session        │
          └───────────────────────────────────┘
```

## 6. Moduły

### `SightAdapt.Accessibility.Uia`

- wątek dedykowany UI Automation;
- kolejka zapytań;
- timeout;
- cache elementów;
- mapowanie runtime ID;
- subskrypcja zmiany fokusu;
- bezpieczne odpinanie handlerów;
- ochrona przed zawieszonym providerem.

### `SightAdapt.Accessibility.Ocr`

- przechwytywanie wycinka;
- redukcja rozdzielczości, jeśli potrzebna;
- lokalny OCR;
- analiza prostokątów słów;
- cache;
- anulowanie;
- polityka prywatności.

### `SightAdapt.Accessibility.Speech`

- kolejka wypowiedzi;
- priorytety komunikatów;
- przerywanie;
- ustawienia głosu;
- obsługa skrótów.

### `SightAdapt.Accessibility.Focus`

- agregacja danych UIA i WinEvent;
- stabilizacja położenia;
- filtrowanie krótkotrwałych zmian;
- rysowanie ramki;
- opisy tekstowe.

### `SightAdapt.Magnifier`

- wybór obszaru źródłowego;
- skalowanie GPU;
- filtr nearest/linear;
- tryb wyostrzenia;
- panel przypięty lub pod kursorem.

## 7. Wieloprofilowość

### 7.1 Zakresy

Profil może działać:

- dla aktywnego okna;
- dla wszystkich okien procesu;
- dla konkretnej klasy okna;
- dla dialogów potomnych;
- dla określonego monitora;
- tylko w określonych godzinach;
- tylko po ręcznym aktywowaniu.

### 7.2 Dziedziczenie

Model docelowy:

```text
Profil bazowy
  └── Profil aplikacji
       └── Profil konkretnego okna
            └── Tymczasowa korekta sesji
```

Należy unikać zbyt skomplikowanego dziedziczenia w pierwszej wersji Hard.

### 7.3 Priorytety reguł

1. jawne wyłączenie;
2. reguła konkretnego okna;
3. reguła pełnej ścieżki EXE;
4. reguła procesu;
5. reguła klasy okna;
6. profil domyślny.

## 8. UIAccess i uprawnienia

Pełna współpraca z aplikacjami uruchomionymi jako administrator może wymagać:

- podpisanego pliku wykonywalnego;
- instalacji w bezpiecznym katalogu;
- manifestu `uiAccess=true`;
- osobnego procesu dostępnościowego.

Proponowany model:

```text
SightAdapt.App
    │
    ├── zwykły interfejs i renderer
    │
    └── opcjonalny podpisany broker UIAccess
```

Broker:

- nie renderuje obrazu;
- wykonuje wyłącznie ograniczone operacje dostępnościowe;
- komunikuje się lokalnie przez nazwane potoki;
- waliduje każde żądanie;
- ma minimalny zakres kodu.

Wersja Light nie wymaga brokera.

## 9. Procesy i niezawodność

Rekomendowany podział w wersji Hard:

- proces główny: tray, profile, orkiestracja;
- proces renderera: nakładki i Direct3D;
- opcjonalny proces UIAccess;
- opcjonalny proces OCR.

Korzyści:

- awaria OCR nie zamyka renderera;
- awaria renderera nie usuwa ustawień;
- można restartować moduły;
- łatwiejsze ograniczanie uprawnień;
- prostsze monitorowanie zużycia pamięci.

Podział na procesy należy wprowadzić dopiero, gdy pomiary uzasadnią złożoność.

## 10. Kompozycja nakładek

Warstwy:

1. przetworzony obraz aplikacji;
2. przyciemnienie;
3. linijka;
4. ramka fokusu;
5. kursor lub halo;
6. panel tekstowy;
7. lupa;
8. komunikaty systemowe aplikacji.

Warstwy powinny być kompozytowane przez GPU.

Nie należy tworzyć osobnego top-level HWND dla każdego prostego elementu, jeśli można użyć jednej powierzchni kompozytowej.

## 11. Interakcja

Domyślnie wszystkie warstwy są click-through.

Tryby interaktywne:

- edycja położenia panelu;
- zmiana rozmiaru lupy;
- wybór obszaru OCR;
- edycja profilu.

Tryb interaktywny musi być jawnie sygnalizowany i automatycznie wyłączany po zakończeniu operacji.

## 12. API wewnętrzne

Przykładowe interfejsy:

```csharp
public interface IAccessibilityFeature
{
    string Id { get; }
    Task StartAsync(AccessibilityContext context, CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}

public interface IFocusedElementSource
{
    event EventHandler<FocusedElementChangedEventArgs> FocusedElementChanged;
    Task<AccessibleElement?> GetCurrentAsync(CancellationToken cancellationToken);
}

public interface ITextSource
{
    Task<TextSnapshot?> ReadAsync(TextRequest request, CancellationToken cancellationToken);
}
```

Moduły nie mogą bezpośrednio zależeć od WPF.

## 13. Telemetria i diagnostyka

Domyślnie:

- brak telemetrii;
- lokalny log;
- możliwość ręcznego wygenerowania raportu diagnostycznego;
- raport bez zrzutów ekranu;
- użytkownik wybiera, czy dołączyć nazwy aplikacji;
- identyfikatory okien nie są traktowane jako trwałe.

Opcjonalna telemetria może pojawić się tylko jako jawne opt-in.

## 14. Dostępność własnego interfejsu

Aplikacja wspierająca dostępność sama musi być dostępna.

Wymagania:

- pełna obsługa klawiatury;
- poprawne etykiety UI Automation;
- skalowanie tekstu;
- wysoki kontrast;
- brak informacji przekazywanej wyłącznie kolorem;
- czytelny fokus;
- możliwość zmiany skrótów;
- możliwość wyłączenia animacji;
- prosty tryb konfiguracji;
- możliwość eksportu ustawień;
- komunikaty błędów z instrukcją działania.

## 15. Współpraca z użytkownikami

Projekt powinien być rozwijany z udziałem osób niedowidzących.

Rekomendowane elementy procesu:

- publiczne scenariusze testowe;
- ankiety jakości profili;
- testy użyteczności;
- issue templates dotyczące dostępności;
- możliwość publikowania profili społecznościowych;
- oddzielenie opinii o funkcji od danych medycznych;
- brak zbierania diagnoz użytkowników.

## 16. Bezpieczeństwo

### 16.1 Model zagrożeń

Należy przeanalizować:

- przechwycenie wrażliwego obrazu;
- złośliwy plik LUT;
- ReDoS przez wyrażenie regularne tytułu;
- zawieszony provider UI Automation;
- spoofing okna;
- przejęcie lokalnego IPC;
- podmianę pliku profilu;
- nieautoryzowane uruchomienie brokera UIAccess;
- pozostawienie nakładki po awarii;
- przekroczenie pamięci GPU.

### 16.2 Zabezpieczenia

- walidacja wszystkich plików;
- limity rozmiaru;
- timeouty;
- podpisane wydania;
- sumy SHA-256;
- ograniczone ACL dla IPC;
- brak wykonywalnych skryptów w profilach;
- brak dynamicznego ładowania niepodpisanych bibliotek;
- bezpieczny tryb uruchomienia bez profili;
- awaryjne wyłączenie z linii poleceń.

## 17. Rozszerzenia i pluginy

Wtyczki nie są rekomendowane w początkowej wersji Hard.

Jeśli zostaną dodane:

- osobny proces hosta;
- jawne uprawnienia;
- podpis lub lista zaufania;
- stabilne API;
- wersjonowanie;
- możliwość wyłączenia;
- zakaz bezpośredniego dostępu do rendererów;
- brak wykonywania wtyczki w procesie UIAccess.

Najbezpieczniejszym pierwszym mechanizmem rozszerzania są:

- profile JSON;
- LUT;
- predefiniowane efekty;
- deklaratywne reguły.

## 18. Testy Hard

Dodatkowe testy:

- aplikacje z poprawnym UI Automation;
- aplikacje z błędnym UI Automation;
- aplikacje bez UI Automation;
- duże drzewa Chromium;
- zablokowane providery;
- aplikacje administratora;
- wiele aktywnych okien;
- częste zmiany fokusu;
- OCR w wielu językach;
- łączenie lupy, LUT i ramki;
- restart pojedynczego modułu;
- wyłączenie wszystkich modułów skrótem awaryjnym;
- zachowanie na Windows 10 i Windows 11.

## 19. Etapy rozwoju Hard

### Hard 0.1

- ramka fokusu;
- panel nazwy aktywnej kontrolki;
- odczyt prostokąta UI Automation;
- jedna lupa przypięta;
- brak OCR.

### Hard 0.2

- panel tekstowy;
- TextPattern;
- linijka czytania;
- profile warstw;
- cache UI Automation.

### Hard 0.3

- lokalny OCR;
- wybór obszaru;
- wykrywanie tekstu z aplikacji custom-rendered;
- opcjonalna synteza mowy.

### Hard 0.4

- broker UIAccess;
- obsługa aplikacji podwyższonych;
- rozbudowane reguły;
- stabilizacja wielu procesów.

### Hard 1.0

- publiczny, stabilny zestaw funkcji;
- pełna dokumentacja;
- polityka bezpieczeństwa;
- testy z użytkownikami;
- profil kompatybilności Windows 10;
- profil główny Windows 11.

## 20. Kryteria wejścia do prac Hard

Prace nad Hard mogą rozpocząć się, gdy Light:

- spełnia pełną definicję ukończenia;
- ma stabilny publiczny interfejs profili;
- ma stabilny renderer;
- nie ma krytycznych błędów nakładki;
- ma testy wielomonitorowe;
- ma działający recovery po utracie urządzenia GPU;
- ma wyniki testów z użytkownikami;
- ma co najmniej jedno stabilne wydanie publiczne.

## 21. Kryteria ukończenia Hard

Wersję Hard 1.0 można uznać za gotową, gdy:

- każda funkcja może zostać osobno wyłączona;
- awaria UI Automation nie wpływa na transformację kolorów;
- awaria OCR nie wpływa na interfejs;
- podstawowe funkcje działają bez administratora;
- skrót awaryjny wyłącza wszystkie warstwy;
- panel ustawień jest dostępny z klawiatury i czytnika ekranu;
- wszystkie moduły mają timeouty i anulowanie;
- istnieje jawna lista ograniczeń;
- aplikacja nie zapisuje obrazu bez świadomej akcji użytkownika;
- projekt przeszedł testy Windows 10 i Windows 11;
- funkcje zostały zweryfikowane z użytkownikami docelowymi.
