using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

namespace RealDungeon
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await GetLocationAndCenterMapAsync();
        }

        private async Task GetLocationAndCenterMapAsync()
        {
            try
            {
                // Zkontrolujeme a případně požádáme o oprávnění
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                }

                // Pokud máme oprávnění, získáme polohu
                if (status == PermissionStatus.Granted)
                {
                    var location = await Geolocation.GetLocationAsync(new GeolocationRequest
                    {
                        DesiredAccuracy = GeolocationAccuracy.Medium,
                        Timeout = TimeSpan.FromSeconds(30)
                    });

                    if (location != null)
                    {
                        // Vycentrujeme mapu na aktuální polohu
                        map.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromKilometers(1)));

                        // PŘIDÁNÍ KOLEČKA PRO NEJDEK ---

                        // Vytvoření nového objektu Circle
                        var nejdekCircle = new Circle
                        {
                            // Nastavení středu kruhu na souřadnice Nejdku
                            Center = new Location(50.322, 12.729),

                            // Poloměr kruhu (např. 500 metrů)
                            Radius = Distance.FromMeters(500),

                            // Barva okraje kruhu
                            StrokeColor = Colors.Red,

                            // Tloušťka okraje
                            StrokeWidth = 8,

                            // Barva výplně kruhu (s průhledností)
                            FillColor = Color.FromRgba(255, 0, 0, 60)
                        };

                        // Přidání vytvořeného kolečka do mapy
                        map.MapElements.Add(nejdekCircle);

                        // --- KONEC NOVÉ ČÁSTI ---
                    }
                }
                else
                {
                    // Uživatel nepovolil přístup k poloze
                    await DisplayAlert("Chyba", "Pro zobrazení polohy na mapě je nutné povolit přístup k poloze.", "OK");
                }
            }
            catch (Exception ex)
            {
                // Zpracování případných chyb
                await DisplayAlert("Chyba při získávání polohy", ex.Message, "OK");
            }
        }
    }
}