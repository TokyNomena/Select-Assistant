using System.Diagnostics;

namespace SelectionSet.ViewModels
{
    public partial class AboutViewModel : ObservableObject
    {
        [ObservableProperty]
        private string productName = "Select Assistant - Outils de Sélection Avancée pour Revit";

        [ObservableProperty]
        private string versionInfo = "Version 1.0.0 | Licence : GRATUITE | © 2025";

        [ObservableProperty]
        private string developerName = "Développé par TOKY NOMENA Andriamparany";

        [ObservableProperty]
        private string purposeStatement =
            "Select Assist est conçu pour optimiser significativement le flux de travail des professionnels du BIM en offrant des méthodes de sélection d'éléments non disponibles nativement dans Autodesk Revit.";

        [ObservableProperty]
        private string technicalExpertise =
            "Méthodes disponibles:\n" +
            "    • Filtrer par paramètres\n" +
            "    • Filtrer par pièces\n" +
            "    • Filtrer dans la vue active\n" +
            "    • Filtrer par étages";

        [ObservableProperty]
        private string linkedInUrl = "https://www.linkedin.com/in/andriamparany-toky-nomena-1bb82a157";

        [ObservableProperty]
        private string emailAddress = "tokynom13@gmail.com";

        [ObservableProperty]
        private string whatsappNumber = "+261343662757";

        [RelayCommand]
        private void OpenLink(string url)
        {

            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch { /* Log l'erreur */ }
        }

        // Commande pour le bouton WhatsApp
        [RelayCommand]
        private void ContactWhatsApp()
        {
            OpenLink($"https://wa.me/{WhatsappNumber.Replace("+", "").Replace(" ", "")}");
        }

        [RelayCommand]
        private void ContactEmail()
        {
            OpenLink($"mailto:{EmailAddress}");
        }
    }
}
