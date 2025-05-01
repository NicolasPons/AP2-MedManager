
using MedManager.Data;
using MedManager.Models;
using MedManager.ViewModel.OrdonnanceVM;
using MedManager.ViewModel.PatientVM;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using X.PagedList;

namespace MedManager.Controllers
{
	[Authorize]
	public class OrdonnanceController : Controller
	{
		private readonly ApplicationDbContext _dbContext;
		private readonly UserManager<Medecin> _userManager;
		private readonly ILogger<Patient> _logger;
		private readonly string _RepertoirePdf = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "pdfs");

		public OrdonnanceController(ApplicationDbContext dbContext, UserManager<Medecin> userManager, ILogger<Patient> logger)
		{
			_logger = logger;
			_dbContext = dbContext;
			_userManager = userManager;
		}

		private async Task<Medecin> ObtenirMedecin()
		{
			var MedecinId = _userManager.GetUserId(User);
			if (MedecinId == null)
			{
				throw new InvalidOperationException("Utilisateur non authentifié");
			}

			var medecin = await _dbContext.Users
				.Include(u => u.Ordonnances)
					.ThenInclude(o => o.Patient)
				.Include(u => u.Ordonnances)
					.ThenInclude(o => o.Medicaments)
				.FirstOrDefaultAsync(m => m.Id == MedecinId);

			return medecin ?? throw new InvalidOperationException("Médecin non trouvé");
		}

		private async Task<Patient> ObtenierPatient(int? patientId)
		{
			return await _dbContext.Patients
				.Include(p => p.Allergies)
				.Include(p => p.Antecedents)
				.FirstOrDefaultAsync(p => p.PatientId == patientId)
				?? throw new InvalidOperationException("Patient non trouvé");
		}

		private async Task<List<Medicament>> ObtenirMedicamentsCompatibles(Patient patient)
		{
			var AllergiesPatient = patient.Allergies.Select(p => p.AllergieId).ToList();
			var AntecedentPatient = patient.Antecedents.Select(p => p.AntecedentId).ToList();

			return await _dbContext.Medicaments
				.Where(m => !m.Allergies.Any(a => AllergiesPatient.Contains(a.AllergieId)) &&
							!m.Antecedents.Any(a => AntecedentPatient.Contains(a.AntecedentId)))
				.ToListAsync();
		}

		private async Task<List<Medicament>> ObtenierMedicamentsIncompatibles(Patient patient)
		{
			var AllergiesPatient = patient.Allergies.Select(p => p.AllergieId).ToList();
			var AntecedentPatient = patient.Antecedents.Select(p => p.AntecedentId).ToList();

			return await _dbContext.Medicaments
				.Where(m => m.Allergies.Any(a => AllergiesPatient.Contains(a.AllergieId)) ||
							m.Antecedents.Any(a => AntecedentPatient.Contains(a.AntecedentId)))
				.ToListAsync();
		}

		private IActionResult HandleException(Exception ex, string errorMessage, string logMessage, int? id = null)
		{
			if (ex is DbException dbEx)
			{
				_logger.LogError(dbEx, logMessage, id);
				TempData["ErrorMessage"] = errorMessage;
				return RedirectToAction("Index", "Error");
			}

			_logger.LogError(ex, "Une erreur inattendue s'est produite.");
			TempData["ErrorMessage"] = "Une erreur inattendue s'est produite. Veuillez réessayer.";
			return RedirectToAction("Index", "Ordonnance");
		}

		private OrdonnanceViewModel PrepareOrdonnanceViewModel(Patient patient, List<Medicament> compatibleMedicaments, List<Medicament> incompatibleMedicaments)
		{
			return new OrdonnanceViewModel
			{
				MedicamentsCompatibles = compatibleMedicaments,
				MedicamentsIncompatibles = incompatibleMedicaments,
				MedicamentIdSelectionnes = new List<int>(),
				PatientId = patient.PatientId,
				NomComplet = patient.NomComplet,
			};
		}

		public async Task<IActionResult> Index(int? page)
		{
			try
			{
				var medecin = await ObtenirMedecin();
				var ordonnances = medecin.Ordonnances.AsQueryable();



				int TaillePage = 9;
				int NombrePage = (page ?? 1);
				var ListePageeDesOrdonnances = ordonnances.ToPagedList(NombrePage, TaillePage);

				var viewModel = new IndexOrdonnanceViewModel
				{
					medecin = medecin,
					Ordonnances = ListePageeDesOrdonnances
				};
				return View(viewModel);
			}
			catch (Exception ex)
			{
				return HandleException(ex, "Une erreur s'est produite lors de la récupération des données.", "Erreur lors de la récupération des données.");
			}
		}

		[HttpGet]
		public async Task<IActionResult> SelectionPatient()
		{
			try
			{
				var medecin = await ObtenirMedecin();

				var modele = new SelectionPatientViewModel
				{
					Patients = await _dbContext.Patients.Where(p => p.MedecinID == medecin.Id).ToListAsync()
				};

				return View(modele);
			}
			catch (Exception ex)
			{
				return HandleException(ex, "Une erreur s'est produite lors de la récupération des patients.", "Erreur lors de la récupération des patients.");
			}
		}

		[HttpPost]
		public async Task<IActionResult> SelectionPatient(SelectionPatientViewModel modele)
		{
			try
			{
				var medecin = await ObtenirMedecin();

				if (ModelState.IsValid)
				{
					var patient = await ObtenierPatient(modele.PatientId);
					var ListeMedicament = await ObtenirMedicamentsCompatibles(patient);

					if (ListeMedicament.Count == 0)
					{
						ModelState.AddModelError("", "Aucun médicament compatible n'a été trouvé pour ce patient.");
						modele.Patients = medecin.Patients;
						return View(modele);
					}

					return RedirectToAction("Ajouter", new { id = patient.PatientId });
				}

				modele.Patients = medecin.Patients;
				return View(modele);
			}
			catch (Exception ex)
			{
				return HandleException(ex, "Une erreur s'est produite lors de la sélection du patient.", "Erreur lors de la sélection du patient.");
			}
		}

		[HttpGet]
		public async Task<IActionResult> Ajouter(int id)
		{
			try
			{
				var patient = await ObtenierPatient(id);
				var ListeMedicamentsCompatibles = await ObtenirMedicamentsCompatibles(patient);
				var ListeMedicamentsIncompatibles = await ObtenierMedicamentsIncompatibles(patient);

				if (ListeMedicamentsCompatibles.Count == 0)
				{
					TempData["ErrorMessage"] = "Aucun médicament compatible n'est disponible pour ce patient.";
					return RedirectToAction("Index", "Patient");
				}

				var model = PrepareOrdonnanceViewModel(patient, ListeMedicamentsCompatibles, ListeMedicamentsIncompatibles);
				return View("Action", model);
			}
			catch (Exception ex)
			{
				return HandleException(ex, "Une erreur s'est produite lors de la récupération des médicaments.", "Erreur lors de la récupération des médicaments.");
			}
		}

		[HttpPost]
		public async Task<IActionResult> Ajouter(OrdonnanceViewModel model)
		{
			try
			{
				var MedecinId = _userManager.GetUserId(User);
				if (MedecinId == null)
				{
					return RedirectToAction("Connexion", "Compte");
				}

				if (!ModelState.IsValid)
				{
					var patient = await ObtenierPatient(model.PatientId);
					var ListeMedicamentCompatible = await ObtenirMedicamentsCompatibles(patient);
					var ListeMedicamentsIncompatibles = await ObtenierMedicamentsIncompatibles(patient);

					model.MedicamentsIncompatibles = ListeMedicamentsIncompatibles;
					model.MedicamentsCompatibles = ListeMedicamentCompatible;
					return View("Action", model);
				}

				if (model.DateDebut > model.DateFin)
				{
					ModelState.AddModelError("DateFin", "La date de fin doit être supérieure à la date de début.");
					var patient = await ObtenierPatient(model.PatientId);
					var ListeMedicamentCompatible = await ObtenirMedicamentsCompatibles(patient);
					var ListeMedicamentsIncompatibles = await ObtenierMedicamentsIncompatibles(patient);

					model.MedicamentsIncompatibles = ListeMedicamentsIncompatibles;
					model.MedicamentsCompatibles = ListeMedicamentCompatible;
					return View("Action", model);
				}

				var medecin = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == MedecinId);
				var Patient = await _dbContext.Patients.FirstOrDefaultAsync(p => p.PatientId == model.PatientId);

				if (medecin == null || Patient == null)
					return NotFound();

				var ordonnance = new Ordonnance
				{
					DateDebut = model.DateDebut,
					DateFin = model.DateFin,
					InfoSupplementaire = model.InfoSupplementaire,
					Medecin = medecin,
					Patient = Patient,
					PatientId = model.PatientId,
					Medicaments = new List<Medicament>(),
					MedecinId = MedecinId,
				};

				if (model.MedicamentIdSelectionnes != null && model.MedicamentIdSelectionnes.Count > 0)
				{
					var MedicamentsSelectionnes = await _dbContext.Medicaments
							.Where(m => model.MedicamentIdSelectionnes.Contains(m.MedicamentId))
							.ToListAsync();
					foreach (var medicament in MedicamentsSelectionnes)
					{
						ordonnance.Medicaments.Add(medicament);
					}
				}

				await _dbContext.Ordonnances.AddAsync(ordonnance);
				await _dbContext.SaveChangesAsync();
				TempData["SuccessMessage"] = "L'ordonnance a été ajoutée avec succès.";
				return RedirectToAction("Index", "Ordonnance");
			}
			catch (DbUpdateException ex)
			{
				return HandleException(ex, "Une erreur s'est produite lors de l'ajout de l'ordonnance.", "Erreur lors de l'ajout de l'ordonnance.");
			}
			catch (Exception ex)
			{
				return HandleException(ex, "Une erreur inattendue s'est produite lors de l'ajout de l'ordonnance.", "Erreur lors de l'ajout de l'ordonnance.");
			}
		}

		[HttpGet]
		public async Task<IActionResult> Editer(int id)
		{
			try
			{
				var MedecinId = _userManager.GetUserId(User);
				if (MedecinId == null)
				{
					return RedirectToAction("Connexion", "Compte");
				}

				var ordonnance = await _dbContext.Ordonnances
						.Include(o => o.Medicaments)
						.Include(o => o.Patient)
						.FirstOrDefaultAsync(o => o.OrdonnanceId == id);

				if (ordonnance == null)
					return NotFound();

				var patient = await ObtenierPatient(ordonnance.Patient.PatientId);
				var ListeMedicamentsCompatibles = await ObtenirMedicamentsCompatibles(patient);
				var ListeMedicamentsIncompatibles = await ObtenierMedicamentsIncompatibles(patient);

				var model = new OrdonnanceViewModel
				{
					OrdonnanceId = ordonnance.OrdonnanceId,
					DateDebut = ordonnance.DateDebut,
					DateFin = ordonnance.DateFin,
					InfoSupplementaire = ordonnance.InfoSupplementaire,
					MedicamentIdSelectionnes = ordonnance.Medicaments.Select(m => m.MedicamentId).ToList(),
					MedicamentsCompatibles = ListeMedicamentsCompatibles,
					MedicamentsIncompatibles = ListeMedicamentsIncompatibles,
					PatientId = ordonnance.Patient.PatientId,
					NomComplet = ordonnance.Patient.NomComplet
				};

				return View("Action", model);
			}
			catch (Exception ex)
			{
				return HandleException(ex, "Une erreur s'est produite lors de la récupération de l'ordonnance.", "Erreur lors de la récupération de l'ordonnance.", id);
			}
		}

		[HttpPost]
		public async Task<IActionResult> Editer(OrdonnanceViewModel model)
		{
			try
			{
				var MedecinId = _userManager.GetUserId(User);
				if (MedecinId == null)
				{
					return RedirectToAction("Connexion", "Compte");
				}

				if (ModelState.IsValid)
				{
					var ordonnance = await _dbContext.Ordonnances
						.Include(p => p.Medicaments)
						.Include(p => p.Patient)
						.FirstOrDefaultAsync(o => o.OrdonnanceId == model.OrdonnanceId);
					var patient = await _dbContext.Patients.FirstOrDefaultAsync(p => p.PatientId == model.PatientId);
					if (ordonnance == null || patient == null)
					{
						return NotFound();
					}
					ordonnance.DateDebut = model.DateDebut;
					ordonnance.DateFin = model.DateFin;
					ordonnance.Patient = patient;
					ordonnance.InfoSupplementaire = model.InfoSupplementaire;

					ordonnance.Medicaments.Clear();
					if (model.MedicamentIdSelectionnes != null)
					{
						var MedicamentSelectionne = await _dbContext.Medicaments
							.Where(m => model.MedicamentIdSelectionnes.Contains(m.MedicamentId))
							.ToListAsync();

						foreach (var medicament in MedicamentSelectionne)
						{
							ordonnance.Medicaments.Add(medicament);
						}
					}

					_dbContext.Entry(ordonnance).State = EntityState.Modified;
					await _dbContext.SaveChangesAsync();
					TempData["SuccessMessage"] = "L'ordonnance a été modifiée avec succès.";
					return RedirectToAction("Index", "Ordonnance");
				}
				else
				{
					var patient = await ObtenierPatient(model.PatientId);
					var ListeMedicamentsCompatibles = await ObtenirMedicamentsCompatibles(patient);
					var ListeMedicamentsIncompatibles = await ObtenierMedicamentsIncompatibles(patient);
					model.MedicamentsIncompatibles = ListeMedicamentsIncompatibles;
					model.MedicamentsCompatibles = ListeMedicamentsIncompatibles;
					return View("Action", model);
				}

			}
			catch (Exception ex)
			{
				return HandleException(ex, "Une erreur s'est produite lors de la modification de l'ordonnance", "Erreur lors de la modification de l'ordonnance.", model.OrdonnanceId);

			}
		}

		public async Task<IActionResult> Supprimer (int id)
		{
			try
			{
				Ordonnance? OrdonnanceSupprimee = await _dbContext.Ordonnances.FindAsync(id);
				if (OrdonnanceSupprimee != null)
				{
					_dbContext.Remove(OrdonnanceSupprimee);
					await _dbContext.SaveChangesAsync();
					TempData["SuccessMessage"] = "L'ordonnance a été supprimée avec succès.";
					return RedirectToAction("Index", "Ordonnance");
				}
				return NotFound();
			}
			catch (Exception ex)
			{
				return HandleException(ex, "Une erreur s'est produite lors de la suppression de l'ordonnance", "Erreur lors de la suppression de l'ordonnance.", id);
			}
		}

		public async Task<IActionResult> GenererPdf(int OrdonnanceId, int PatientId)
		{
			try
			{

			var medecin = await ObtenirMedecin();
			var ordonnance = _dbContext.Ordonnances
						.Include(o => o.Medicaments)
						.FirstOrDefault(o => o.OrdonnanceId == OrdonnanceId);
			var patient = await _dbContext.Patients.FirstOrDefaultAsync(p => p.PatientId == PatientId);

			if (patient != null && medecin != null && ordonnance != null)
			{
				string fileName = $"Ordonnance_{patient.NomComplet}_{ordonnance.OrdonnanceId}.pdf";
				string filePath = Path.Combine(_RepertoirePdf, fileName);

				var pdfGenerateur = new OrdonnancePdfGenerateur();

				pdfGenerateur.GenerateOrdonnance(filePath, medecin, patient, ordonnance);

				if (!System.IO.File.Exists(filePath))
				{
					return NotFound("Le fichier PDF n'a pas pu être créé.");
				}

				var fileBytes = System.IO.File.ReadAllBytes(filePath);
				return File(fileBytes, "application/pdf", fileName);
			}
			else
				return NotFound();
			}
			catch (Exception ex)
			{
				return HandleException(ex, "Une erreur s'est produite lors de la génération du PDF.", "Erreur lors de la génération du PDF.", OrdonnanceId);
			}
		}

		public async Task<IActionResult> Detail(int id)
		{
			try
			{
				var MedecinId = _userManager.GetUserId(User);

				var ordo = await _dbContext.Ordonnances
								.Include(o => o.Medicaments)
								.Include(o => o.Patient)
								.FirstOrDefaultAsync(o => o.OrdonnanceId == id);
				if (ordo == null)
					return NotFound();
				return View(ordo);
			}
			catch (Exception ex)
			{
				return HandleException(ex, "Une erreur s'est produite lors de la récupération de l'ordonnance.", "Erreur lors de la récupération de l'ordonnance.", id);
			}
		}
	}
}
