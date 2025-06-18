using MedManager.Models;
namespace MedManager.ViewModel.MedecinVM
{

    public class TableauDeBordViewModel
    {
        public List<Frequence>? FrequenceAllergies { get; set; }
        public List<Frequence>? FrequenceAntecedents { get; set; }
        public List<RepartitionAge>? RepartitionAges { get; set; }
        public List<MedicamentUtilisationViewModel>? MedicamentPlusUtilises { get; set; }
        public List<Patient>? CinqDerniersPatient { get; set; }
        public List<Ordonnance>? CinqDerniersOrdo { get; set; }
        public int TotalPatient { get; set; }
        public int TotalOrdonnance { get; set; }

        public List<Ordonnance>? OrdonnanceEnCours { get; set; }
        public List<RepartitionIMC>? RepartitionIMCs { get; set; }
        
        public List<MedecinsPlusDePatients>? Top5Medecins { get; set;  }
    }
}
