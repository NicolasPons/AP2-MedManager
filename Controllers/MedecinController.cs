using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MedManager.Models;
using Microsoft.AspNetCore.Identity;
using MedManager.Data;
using Microsoft.EntityFrameworkCore;
using X.PagedList;
using MedManager.ViewModel;
using MedManager.ViewModel.MedecinVM;

namespace MedManager.Controllers;

public class MedecinController : Controller
{
	private readonly ILogger<Medecin> _logger;
	private readonly UserManager<Medecin> _userManager;
	private readonly ApplicationDbContext _dbContext;

	public MedecinController(ILogger<Medecin> logger, UserManager<Medecin> userManager, ApplicationDbContext dbContext)
	{
		_logger = logger;
		_userManager = userManager;
		_dbContext = dbContext;
	}

	public IActionResult Index()
	{
		return View();
	}

    public async Task<IActionResult> TableauBord()
    {
        var medocs = await ObtenirMedicamentLesPlusUtilises();
		var frequenceAllergies = await ObtenirAllergiesLesPlusFrequentes();
		var frequenceAntecedents = await ObtenirAntecedentsLesPlusFrequentes();
		var repartitionAge = await ObtenirRepartitionAge();

		var model = new TableauDeBordViewModel
		{
			FrequenceAllergies = frequenceAllergies,
			FrequenceAntecedents = frequenceAntecedents,
			RepartitionAges = repartitionAge, 
			MedicamentPlusUtilises = medocs
        };
        return View(model);
    }


    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	public IActionResult Error()
	{
		return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
	}

	private async Task<string?> ObtenirIdMedecin()
	{
		var user = await _userManager.GetUserAsync(User);
		if (user != null)
		{
			string id = user.Id;
			return id;
		}
		return null;
	}

	private async Task<List<MedicamentUtilisationViewModel>> ObtenirMedicamentLesPlusUtilises()
	{
		var id = await ObtenirIdMedecin();

		if (id != null)
		{
			var ordonnances = await _dbContext.Ordonnances
								.Include(o => o.Medicaments)
								.Where(o => o.MedecinId == id)
								.ToListAsync();

			var medicaments = ordonnances
								.SelectMany(o => o.Medicaments)
								.GroupBy(m => m.MedicamentId)
								.Select(g => new MedicamentUtilisationViewModel
								{
									Nom = g.First().Nom,
									UtilisationCount = g.Count()
								})
								.OrderByDescending(m => m.UtilisationCount)
								.Take(5)
								.ToList();

			return medicaments;
		}
		return new List<MedicamentUtilisationViewModel>();
	}

	private async Task<List<FrequenceAllergie>> ObtenirAllergiesLesPlusFrequentes()
	{
		var id = await ObtenirIdMedecin();
		if (id != null)
		{
			var patients = await _dbContext.Patients
							.Include(p => p.Allergies)
							.Where(p => p.MedecinID == id)
							.ToListAsync();

			var allergies = patients
				.SelectMany(p => p.Allergies)
				.GroupBy(a => a.AllergieId)
				.Select(g => new FrequenceAllergie
				{
					nom = g.First().Nom,
					compte = g.Count()
				})
				.OrderByDescending(a => a.compte)
				.Take(5)
				.ToList();
			return allergies;
		}
		return new List<FrequenceAllergie>();
	}

    private async Task<List<FrequenceAntecedent>> ObtenirAntecedentsLesPlusFrequentes()
    {
        var id = await ObtenirIdMedecin();
        if (id != null)
        {
            var patients = await _dbContext.Patients
                            .Include(p => p.Antecedents)
                            .Where(p => p.MedecinID == id)
                            .ToListAsync();

            var antecedents = patients
                .SelectMany(p => p.Antecedents)
                .GroupBy(a => a.AntecedentId)
                .Select(g => new FrequenceAntecedent
                {
                    nom = g.First().Nom,
                    compte = g.Count()
                })
                .OrderByDescending(a => a.compte)
                .Take(5)
                .ToList();
			return antecedents;
		}
        return new List<FrequenceAntecedent>();
    }

    private async Task<List<RepartitionAge>> ObtenirRepartitionAge()
    {
        var id = await ObtenirIdMedecin();
        if (id != null)
        {
            var patients = await _dbContext.Patients
                            .Include(p => p.Antecedents)
                            .Where(p => p.MedecinID == id)
                            .ToListAsync();

            var repartition = patients
                .GroupBy(p =>
                    p.Age < 18 ? "Mineurs" :
                    p.Age <= 40 ? "Jeunes Adultes" :
                    p.Age <= 60 ? "Adultes" :
                    "Seniors")
                .Select(g => new RepartitionAge
                {
                    categorie = g.Key,
                    compte = g.Count()
                })
                .ToList();

            return repartition;
        }
        return new List<RepartitionAge>();
    }


    private async Task<List<Patient>> ObtenirCinqDerniersPatients()
	{
		var id = await ObtenirIdMedecin();

		if (id != null)
		{
			var patients = await _dbContext.Patients
							.Where(p => p.MedecinID == id)
							.OrderByDescending(p => p.DateCreation)
							.Take(5)
							.ToListAsync();
			return patients;
		}
		return new List<Patient>();
	}

	private async Task<List<Ordonnance>> ObtenirCinqDerniersOrdonnances()
	{
		var id = await ObtenirIdMedecin();

		if (id != null)
		{
			var ordo = await _dbContext.Ordonnances
							.Where(o => o.MedecinId == id)
							.OrderByDescending(p => p.DateCreation)
							.Take(5)
							.ToListAsync();
			return ordo;
		}
		return new List<Ordonnance>();
	}
}
