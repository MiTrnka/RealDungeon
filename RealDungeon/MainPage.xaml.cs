using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

namespace RealDungeon;

public partial class MainPage : ContentPage
{
    // Proměnná pro uložení poslední známé polohy.
    private Location _currentLocation;

    // Vytvoříme si seznam (List), do kterého budeme ukládat jednotlivé body naší trasy.
    // Inicializujeme ho rovnou, aby byl připraven k použití.
    private readonly List<Location> _routePoints = new List<Location>();

    // Uložíme si kruh do proměnné, abychom ho mohli snadno spravovat.
    private Circle _nejdekCircle;

    public MainPage()
    {
        InitializeComponent();
        InitializeNejdekCircle();

        // Tímto řádkem zajistíme, že se po spuštění aplikace v Pickeru
        // vizuálně vybere položka na indexu 2 (Střední přesnost).
        // I když je to nastavené v XAML, toto je pojistka pro spolehlivé zobrazení.
        accuracyPicker.SelectedIndex = 2;

        mapTypePicker.SelectedIndex = 0;

        // Zobrazí na mapě polohu mobilu, funguje, pouze pokud má aplikace oprávnění k poloze.
        map.IsShowingUser = true;
        //map.IsTrafficEnabled = true;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await GetLocationAndCenterMapAsync();
    }

    /// <summary>
    /// Vytvoří a nakonfiguruje objekt kruhu pro Nejdek.
    /// </summary>
    private void InitializeNejdekCircle()
    {
        _nejdekCircle = new Circle
        {
            Center = new Location(50.322, 12.729),
            Radius = Distance.FromMeters(500),
            StrokeColor = Colors.Red,
            StrokeWidth = 8,
            FillColor = Color.FromRgba(255, 0, 0, 60)
        };
    }

    // ========================================================================
    // KÓD Z LEKCE 1: Získávání polohy a centrování mapy
    // ========================================================================

    private async void RecenterMap_Clicked(object sender, EventArgs e)
    {
        await GetLocationAndCenterMapAsync();
    }

    private async Task GetLocationAndCenterMapAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }
            if (status != PermissionStatus.Granted)
            {
                await DisplayAlert("Chybějící oprávnění", "Pro zobrazení polohy je nutné povolit přístup k poloze.", "OK");
                return;
            }

            var accuracy = accuracyPicker.SelectedIndex switch
            {
                0 => GeolocationAccuracy.Lowest,
                1 => GeolocationAccuracy.Low,
                2 => GeolocationAccuracy.Medium,
                3 => GeolocationAccuracy.High,
                4 => GeolocationAccuracy.Best,
                _ => GeolocationAccuracy.Medium
            };

            var request = new GeolocationRequest(accuracy, TimeSpan.FromSeconds(30));

            _currentLocation = await Geolocation.GetLocationAsync(request);

            if (_currentLocation != null)
            {
                map.MoveToRegion(MapSpan.FromCenterAndRadius(_currentLocation, Distance.FromKilometers(0.5)));

                // Smažeme vše (kresby i piny) a znovu přidáme jen kruh.
                ClearDrawing_Clicked(this, EventArgs.Empty);
                map.Pins.Clear();
                map.MapElements.Add(_nejdekCircle);
            }
            else
            {
                await DisplayAlert("Poloha nenalezena", "Nepodařilo se zjistit vaši aktuální polohu.", "OK");
            }
        }
        catch (FeatureNotEnabledException)
        {
            await DisplayAlert("GPS vypnuto", "Prosím, zapněte polohové služby (GPS) ve vašem telefonu.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Chyba při získávání polohy", ex.Message, "OK");
        }
    }

    // ========================================================================
    // KÓD PRO LEKCI 2: Práce se značkami (Pins)
    // ========================================================================

    /// <summary>
    /// Přidá na mapu novou značku (Pin) na aktuálně zjištěnou polohu.
    /// </summary>
    private async void AddPin_Clicked(object sender, EventArgs e)
    {
        if (_currentLocation == null)
        {
            await DisplayAlert("Poloha neznámá", "Nejprve načtěte svoji polohu, prosím.", "OK");
            return;
        }

        // Vytvoříme novou značku (špendlík)
        var pin = new Pin
        {
            Label = "Moje poloha",
            Address = $"Lat: {_currentLocation.Latitude}, Lon: {_currentLocation.Longitude}",
            Location = _currentLocation
        };

        // Přidáme vytvořenou značku na mapu.
        map.Pins.Add(pin);
    }

    /// <summary>
    /// Přidá značku (Pin) do aktuálního vizuálního středu mapy.
    /// Užitečné, když si uživatel posune mapu na místo, které ho zajímá.
    /// </summary>
    private void AddPinToMapCenter_Clicked(object sender, EventArgs e)
    {
        // Z objektu mapy si můžeme jednoduše vytáhnout jeho 'VisibleRegion',
        // což je oblast, kterou uživatel právě vidí.
        // Tento region má vlastnost 'Center', která obsahuje požadované souřadnice.
        var mapCenter = map.VisibleRegion.Center;

        var pin = new Pin
        {
            Label = "Střed mapy",
            // Pro přehlednost vypíšeme souřadnice i do popisku.
            // Používáme formátování čísla na 5 desetinných míst.
            Address = $"Lat: {mapCenter.Latitude:F5}, Lon: {mapCenter.Longitude:F5}",
            Location = mapCenter
        };

        map.Pins.Add(pin);
    }

    /// <summary>
    /// Smaže všechny značky (piny) z mapy.
    /// </summary>
    private void ClearPins_Clicked(object sender, EventArgs e)
    {
        map.Pins.Clear();
    }


    // ========================================================================
    // KÓD PRO LEKCI 3: Kreslení na mapě
    // ========================================================================

    /// <summary>
    /// Přidá souřadnice středu mapy do seznamu bodů a vykreslí/aktualizuje čáru (Polyline).
    /// </summary>
    private void AddRoutePoint_Clicked(object sender, EventArgs e)
    {
        var mapCenter = map.VisibleRegion.Center;
        _routePoints.Add(mapCenter);

        // smažeme staré kresby z mapy a hned vykreslíme novou, delší.
        RemoveDrawingsFromMap();

        var polyline = new Polyline { StrokeColor = Colors.Blue, StrokeWidth = 12 };
        foreach (var point in _routePoints)
        {
            polyline.Geopath.Add(point);
        }
        map.MapElements.Add(polyline);
    }

    /// <summary>
    /// Vykreslí z uložených bodů uzavřený polygon.
    /// </summary>
    private async void DrawPolygon_Clicked(object sender, EventArgs e)
    {
        if (_routePoints.Count < 3)
        {
            await DisplayAlert("Málo bodů", "Pro vykreslení polygonu potřebujete alespoň 3 body v trase.", "OK");
            return;
        }

        // Smažeme staré kresby, ale seznam bodů necháme.
        RemoveDrawingsFromMap();

        var polygon = new Polygon
        {
            StrokeColor = Colors.Green,
            StrokeWidth = 8,
            FillColor = Color.FromRgba(0, 255, 0, 80)
        };

        foreach (var point in _routePoints)
        {
            polygon.Geopath.Add(point);
        }
        map.MapElements.Add(polygon);
    }

    /// <summary>
    /// Smaže POUZE čáry a polygony. Zároveň vyprázdní seznam uložených bodů.
    /// Slouží pro kompletní reset kreslení, ale nechá na mapě ostatní prvky (kruh).
    /// </summary>
    private void ClearDrawing_Clicked(object sender, EventArgs e)
    {
        // Zavoláme naši chytrou metodu, která maže selektivně.
        RemoveDrawingsFromMap();
        // Seznam bodů vyčistíme vždy.
        _routePoints.Clear();
    }

    /// <summary>
    /// Pomocná metoda, která z mapy odstraní pouze prvky typu Polyline a Polygon.
    /// Ostatní prvky (jako náš červený kruh) na mapě zanechá.
    /// </summary>
    private void RemoveDrawingsFromMap()
    {
        // Najdeme všechny prvky, které jsou typu Polyline nebo Polygon.
        var drawings = map.MapElements.Where(el => el is Polyline || el is Polygon).ToList();

        // Projdeme nalezené prvky a jeden po druhém je z mapy smažeme.
        foreach (var drawing in drawings)
        {
            map.MapElements.Remove(drawing);
        }
    }


    // ========================================================================
    // KÓD PRO LEKCI 4: Sledování polohy v reálném čase
    // ========================================================================

    /// <summary>
    /// Metoda se zavolá pokaždé, když uživatel změní stav přepínače pro sledování.
    /// Nyní respektuje přesnost nastavenou v Pickeru.
    /// </summary>
    private async void TrackingSwitch_Toggled(object sender, ToggledEventArgs e)
    {
        bool isToggledOn = e.Value;

        if (isToggledOn)
        {
            // Předtím, než začneme nové sledování, smažeme všechny staré kresby
            // a vyčistíme seznam bodů z předchozí trasy.
            ClearDrawing_Clicked(this, EventArgs.Empty);

            try
            {
                Geolocation.LocationChanged += Geolocation_LocationChanged;

                // OPRAVA: Přebíráme hodnotu přesnosti z Pickeru, stejně jako v Lekci 1.
                // Tím dáváme uživateli plnou kontrolu nad přesností sledování.
                var accuracy = accuracyPicker.SelectedIndex switch
                {
                    0 => GeolocationAccuracy.Lowest,
                    1 => GeolocationAccuracy.Low,
                    2 => GeolocationAccuracy.Medium,
                    3 => GeolocationAccuracy.High,
                    4 => GeolocationAccuracy.Best,
                    _ => GeolocationAccuracy.Medium // Výchozí hodnota pro jistotu
                };

                var request = new GeolocationListeningRequest(accuracy, TimeSpan.FromSeconds(3));

                // Zde spustíme sledování polohy na pozadí s danou přesností.
                await Geolocation.StartListeningForegroundAsync(request);

                // Informujeme uživatele, že sledování bylo úspěšně spuštěno.
                await DisplayAlert("Sledování aktivní", $"Sledování polohy bylo spuštěno s přesností: {accuracy}.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Chyba", $"Nepodařilo se spustit sledování: {ex.Message}", "OK");
                trackingSwitch.IsToggled = false;
            }
        }
        else
        {
            Geolocation.StopListeningForeground();
            Geolocation.LocationChanged -= Geolocation_LocationChanged;
        }
    }

    /// <summary>
    /// Tato metoda je "posluchač" (event handler), který je automaticky volán systémem Android/iOS,
    /// když dojde ke změně polohy zařízení. Děje se tak ale POUZE v případě, že jsme předtím
    /// zapnuli sledování pomocí Geolocation.StartListeningForegroundAsync().
    /// </summary>
    /// <param name="sender">Objekt, který událost vyvolal (v tomto případě Geolocation).</param>
    /// <param name="e">Argumenty události, které obsahují klíčovou informaci - novou polohu.</param>
    void Geolocation_LocationChanged(object sender, GeolocationLocationChangedEventArgs e)
    {
        // 1. ZÍSKÁNÍ NOVÉ POLOHY
        // Z argumentů 'e' si vytáhneme nově zjištěnou polohu.
        var newLocation = e.Location;

        // 2. AKTUALIZACE STAVOVÝCH PROMĚNNÝCH
        // Uložíme si novou polohu do naší hlavní proměnné, aby s ní mohly pracovat
        // i ostatní části aplikace (např. tlačítko "Přidat značku na moji polohu").
        _currentLocation = newLocation;

        // Přidáme nový bod do seznamu pro kreslení trasy. Seznam se tak postupně plní.
        _routePoints.Add(newLocation);

        // 3. PŘEKRESLENÍ TRASY NA MAPĚ
        // Smažeme z mapy starou (kratší) čáru.
        RemoveDrawingsFromMap();

        // Vytvoříme úplně novou čáru, která bude obsahovat všechny body
        // z našeho aktualizovaného seznamu _routePoints.
        var polyline = new Polyline { StrokeColor = Colors.Blue, StrokeWidth = 12 };
        foreach (var point in _routePoints)
        {
            polyline.Geopath.Add(point);
        }
        // Přidáme novou, delší čáru na mapu.
        map.MapElements.Add(polyline);

        // 4. AUTOMATICKÉ CENTROVÁNÍ MAPY
        // Aby byl uživatel stále v obraze, posuneme viditelnou oblast mapy tak,
        // aby její střed byl přesně na naší nové aktuální poloze.
        // Úroveň přiblížení (Distance.FromKilometers(1)) zůstává stejná.
        map.MoveToRegion(MapSpan.FromCenterAndRadius(newLocation, Distance.FromKilometers(0.5)));
    }


    // ========================================================================
    // KÓD PRO LEKCI 5: Geokódování
    // ========================================================================

    /// <summary>
    /// Vezme adresu z textového pole, převede ji na souřadnice (geokódování)
    /// a umístí na toto místo na mapě značku.
    /// </summary>
    private async void Geocode_Clicked(object sender, EventArgs e)
    {
        // Zkontrolujeme, zda uživatel něco zadal do textového pole.
        if (string.IsNullOrWhiteSpace(addressEntry.Text))
        {
            await DisplayAlert("Chybí adresa", "Prosím, zadejte adresu do textového pole.", "OK");
            return;
        }

        try
        {
            // Zavoláme metodu pro geokódování. Jako parametr jí předáme text od uživatele.
            // Tato metoda může vrátit více výsledků (např. pro "Praha" jich bude mnoho).
            var locations = await Geocoding.GetLocationsAsync(addressEntry.Text);

            // Vezmeme si první a (pravděpodobně) nejrelevantnější výsledek.
            // Používáme metodu FirstOrDefault(), která bezpečně vrátí null, pokud žádný výsledek nebyl nalezen.
            var location = locations?.FirstOrDefault();

            if (location != null)
            {
                // Vytvoříme popisek a značku pro nalezené místo.
                var pin = new Pin
                {
                    Label = "Nalezené místo",
                    Address = addressEntry.Text,
                    Location = location
                };
                map.Pins.Add(pin);

                // Posuneme mapu tak, aby se na nalezené místo vycentrovala.
                map.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromKilometers(0.5)));
            }
            else
            {
                await DisplayAlert("Nenalezeno", "Tuto adresu se nepodařilo najít.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Chyba", $"Při geokódování nastala chyba: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Vezme souřadnice středu mapy, převede je na nejbližší adresu (reverzní geokódování)
    /// a zobrazí ji uživateli v informačním okně.
    /// </summary>
    private async void ReverseGeocode_Clicked(object sender, EventArgs e)
    {
        try
        {
            // Získáme souřadnice aktuálního středu mapy.
            var mapCenter = map.VisibleRegion.Center;

            // Zavoláme metodu pro reverzní geokódování.
            // Tato metoda může vrátit více možných popisů místa (Placemark).
            var placemarks = await Geocoding.GetPlacemarksAsync(mapCenter);

            var placemark = placemarks?.FirstOrDefault();

            if (placemark != null)
            {
                // Sestavíme čitelnou adresu z jednotlivých částí, které nám systém vrátil.
                // Ne všechny části musí být vždy dostupné, proto kontrolujeme, zda nejsou prázdné.
                var address =
                    $"Země: {placemark.CountryName}\n" +
                    $"Admin. oblast: {placemark.AdminArea}\n" +
                    $"Město: {placemark.Locality}\n" +
                    $"Ulice: {placemark.Thoroughfare} {placemark.SubThoroughfare}\n" +
                    $"PSČ: {placemark.PostalCode}";

                await DisplayAlert("Nalezená adresa", address, "OK");
            }
            else
            {
                await DisplayAlert("Nenalezeno", "Pro tento bod se nepodařilo zjistit adresu.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Chyba", $"Při reverzním geokódování nastala chyba: {ex.Message}", "OK");
        }
    }


    // ========================================================================
    // KÓD PRO LEKCI 6: Pokročilé funkce a tipy
    // ========================================================================

    /// <summary>
    /// Mění typ mapy podle výběru v Pickeru.
    /// </summary>
    private void MapTypePicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        var picker = (Picker)sender;
        int selectedIndex = picker.SelectedIndex;

        switch (selectedIndex)
        {
            case 0:
                map.MapType = MapType.Street;
                break;
            case 1:
                map.MapType = MapType.Satellite;
                break;
            case 2:
                map.MapType = MapType.Hybrid;
                break;
        }
    }
}