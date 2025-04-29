using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MedManager.Models;
using Microsoft.AspNetCore.Identity;
using MedManager.Data;
using Microsoft.EntityFrameworkCore;
using MedManager.ViewModel.PatientVM;
using Microsoft.AspNetCore.Mvc.Abstractions;
using X.PagedList;
using System.Data.Common;
using Microsoft.CodeAnalysis.CSharp;
using NuGet.Versioning;
using Microsoft.AspNetCore.Authorization;

namespace MedManager.Controllers
{
    [Authorize]
    public class PatientController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<Medecin> _userManager;
        private readonly ILogger<PatientController> _logger;

        public PatientController(ApplicationDbContext dbContext, UserManager<Medecin> userManager, ILogger<PatientController> logger)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int? page, string Filtre, string sortBy = "", string sortDir = "asc")
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Connexion", "Compte");
                }

                string id = user.Id;
                Medecin? medecin = await _dbContext.Users
                    .Include(u => u.Patients)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (medecin == null)
                {
                    return RedirectToAction("Index", "Error");
                }

                var patients = medecin.Patients.AsQueryable();

                if (!string.IsNullOrEmpty(Filtre))
                {
                    patients = patients.Where(p =>
                        (p.Nom != null && p.Nom.Contains(Filtre, StringComparison.OrdinalIgnoreCase)) ||
                        (p.Prenom != null && p.Prenom.Contains(Filtre, StringComparison.OrdinalIgnoreCase)));
                }

                ViewData["ActiveSort"] = sortBy;
                ViewData["SortDir"] = sortDir;

                patients = sortBy.ToLower() switch
                {
                    "nom" => sortDir == "asc" ? patients.OrderBy(p => p.Nom) : patients.OrderByDescending(p => p.Nom),
                    "prenom" => sortDir == "asc" ? patients.OrderBy(p => p.Prenom) : patients.OrderByDescending(p => p.Prenom),
                    "age" => sortDir == "asc" ? patients.OrderBy(p => p.Age) : patients.OrderByDescending(p => p.Age),
                    _ => patients.OrderBy(p => p.Nom)
                };

                int TaillePage = 12;
                int NombrePage = (page ?? 1);
                var ListePagineesPatients = patients.ToPagedList(NombrePage, TaillePage);

                var viewModel = new IndexPatientViewModel
                {
                    Patients = ListePagineesPatients
                };

                ViewData["FiltreActuel"] = Filtre;
                return View(viewModel);
            }
            catch (DbException ex)
            {
                _logger.LogError(ex, "Une erreur s'est produite lors de la récupération des données.");
                return RedirectToAction("Index", "Error");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Une erreur inattendue est survenue.");
                return RedirectToAction("Index", "Error");
            }
        }


		[HttpGet]
        public async Task<IActionResult> Ajouter()
        {
            try
            {
                var viewModel = new PatientViewModel
                {
                    Allergies = await _dbContext.Allergies.ToListAsync(),
                    Antecedents = await _dbContext.Antecedents.ToListAsync(),
                };
                return View(viewModel);
            }
            catch (DbException ex)
            {
                _logger.LogError(ex, "Une erreur s'est produite lors de la récupération des allergies ou des antécédents.");
                return RedirectToAction("Index", "Error");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Une erreur inattendue est survenue.");
                return RedirectToAction("Index", "Error");
            }
        }


        [HttpPost]
        public async Task<IActionResult> Ajouter(PatientViewModel model, IFormFile? photo)
        {
            try
            {

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Connexion", "Compte");
                }

                string id = user.Id;

                Medecin? medecin = await _dbContext.Users.FirstOrDefaultAsync(m => m.Id == id);
                if (medecin == null)
                    return NotFound();

                if (ModelState.IsValid)
                {
                    var patient = new Patient
                    {
                        Nom = model.Nom,
                        Prenom = model.Prenom,
                        NumeroSecuriteSocial = model.NuméroSécuritéSociale,
                        DateNaissance = model.DateNaissance,
                        Taille = model.Taille,
                        Poids = model.Poids,
                        Adresse = model.Adresse,
                        Ville = model.Ville,
                        Sexe = model.Sexe,
                        MedecinID = medecin.Id,
                        medecin = medecin,
                    };

                    if (photo != null && photo.Length > 0)
                    {
                        await using var memoryStream = new MemoryStream();
                        await photo.CopyToAsync(memoryStream);
                        patient.Photo = memoryStream.ToArray();
                    }
                    else 
                    {

						var defaultPhotoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images", "PhotoDefaut-200.png");
						patient.Photo = await System.IO.File.ReadAllBytesAsync(defaultPhotoPath);
					}


                    if (model.AllergieIdSelectionnes != null)
                    {
                        var AllergiesSelectionnes = await _dbContext.Allergies
                            .Where(a => model.AllergieIdSelectionnes.Contains(a.AllergieId))
                            .ToListAsync();
                        foreach (var allergie in AllergiesSelectionnes)
                        {
                            patient.Allergies.Add(allergie);
                        }
                    }

                    if (model.AntecedentIdSelectionnes != null)
                    {
                        var AntecedentsSelectionnes = await _dbContext.Antecedents
                            .Where(a => model.AntecedentIdSelectionnes.Contains(a.AntecedentId))
                            .ToListAsync();
                        foreach (var antecedent in AntecedentsSelectionnes)
                        {
                            patient.Antecedents.Add(antecedent);
                        }
                    }

                    await _dbContext.Patients.AddAsync(patient);
                    await _dbContext.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Le patient a été ajouté avec succès.";
                    return RedirectToAction("Index", "Patient");
                }
                else
                {
					var modele = new PatientViewModel
					{
						Allergies = await _dbContext.Allergies.ToListAsync(),
						Antecedents = await _dbContext.Antecedents.ToListAsync(),
					};
					return View(modele);
				}
			}
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Une erreur s'est produite lors de la mise à jour de la base de données.");
                TempData["ErrorMessage"] = "Une erreur s'est produite lors de l'ajout du patient. Veuillez réessayer.";
                return RedirectToAction("Index", "Error");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Une erreur inattendue est survenue lors de l'ajout du patient.");
                TempData["ErrorMessage"] = "Une erreur inattendue est survenue. Veuillez réessayer.";
                return RedirectToAction("Index", "Error");
            }
        }



        public async Task<IActionResult> Supprimer(int id)
        {
            try
            {
                Patient? patientToDelete = await _dbContext.Patients.FindAsync(id);
                if (patientToDelete != null)
                {
                    _dbContext.Patients.Remove(patientToDelete);
                    await _dbContext.SaveChangesAsync();
					TempData["SuccessMessage"] = "Le patient a été supprimé avec succès.";
					return RedirectToAction("Index", "Patient");
                }
                return NotFound();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Une erreur s'est produite lors de la suppression du patient.");
                return RedirectToAction("Index", "Error");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Une erreur inattendue est survenue lors de la suppression du patient.");
                return RedirectToAction("Index", "Error");
            }
        }


        [HttpGet]
        public async Task<IActionResult> Editer(int id)
        {
            try
            {
                Patient? patient = await _dbContext.Patients
                                                    .Include(p => p.Allergies)
                                                    .Include(p => p.Antecedents)
                                                    .FirstOrDefaultAsync(p => p.PatientId == id);

                if (patient == null)
                    return NotFound();

                var viewModel = new PatientViewModel
                {
                    Nom = patient.Nom,
                    Prenom = patient.Prenom,
                    NuméroSécuritéSociale = patient.NumeroSecuriteSocial,
                    DateNaissance = patient.DateNaissance,
                    Taille = patient.Taille,
                    Poids = patient.Poids,
                    Adresse = patient.Adresse,
                    Ville = patient.Ville,
                    Sexe = patient.Sexe,
                    Photo = patient.Photo,
                    Allergies = await _dbContext.Allergies.ToListAsync(),
                    Antecedents = await _dbContext.Antecedents.ToListAsync(),
                    AllergieIdSelectionnes = patient.Allergies.Select(a => a.AllergieId).ToList(),
                    AntecedentIdSelectionnes = patient.Antecedents.Select(a => a.AntecedentId).ToList()
                };
				return View(viewModel);
			}
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Une erreur s'est produite lors de la récupération des informations du patient.");
                return RedirectToAction("Index", "Error");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Une erreur inattendue est survenue lors de la récupération des informations du patient.");
                return RedirectToAction("Index", "Error");
            }
        }


        [HttpPost]
        public async Task<IActionResult> Editer(int id, PatientViewModel viewModel, IFormFile? photo)
        {
            if (!ModelState.IsValid)
            {
                viewModel.Allergies = await _dbContext.Allergies.ToListAsync();
                viewModel.Antecedents = await _dbContext.Antecedents.ToListAsync();
                return View(viewModel);
            }

            try
            {
                var patient = await _dbContext.Patients
                                              .Include(p => p.Allergies)
                                              .Include(p => p.Antecedents)
                                              .FirstOrDefaultAsync(p => p.PatientId == id);

                if (patient == null)
                {
                    return NotFound();
                }

                patient.Nom = viewModel.Nom;
                patient.Prenom = viewModel.Prenom;
                patient.NumeroSecuriteSocial = viewModel.NuméroSécuritéSociale;
                patient.DateNaissance = viewModel.DateNaissance;
                patient.Taille = viewModel.Taille;
                patient.Poids = viewModel.Poids;
                patient.Adresse = viewModel.Adresse;
                patient.Ville = viewModel.Ville;
                patient.Sexe = viewModel.Sexe;

                if (photo != null && photo.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await photo.CopyToAsync(memoryStream);
                        patient.Photo = memoryStream.ToArray();
                    }
                }
     

                patient.Allergies.Clear();
                if (viewModel.AllergieIdSelectionnes != null)
                {
                    patient.Allergies = await _dbContext.Allergies
                                                        .Where(a => viewModel.AllergieIdSelectionnes.Contains(a.AllergieId))
                                                        .ToListAsync();
                }
                patient.Antecedents.Clear();
                if (viewModel.AntecedentIdSelectionnes != null)
                {
                    patient.Antecedents = await _dbContext.Antecedents
                                                          .Where(a => viewModel.AntecedentIdSelectionnes.Contains(a.AntecedentId))
                                                          .ToListAsync();
                }

                _dbContext.Entry(patient).State = EntityState.Modified;
                await _dbContext.SaveChangesAsync();
				TempData["SuccessMessage"] = "Le patient a été modifié avec succès.";
				return RedirectToAction("Index");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Une erreur s'est produite lors de la mise à jour des informations du patient.");
                return RedirectToAction("Index", "Error");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Une erreur inattendue est survenue lors de la mise à jour des informations du patient.");
                return RedirectToAction("Index", "Error");
            }
        }




        public async Task<IActionResult> Detail(int id)
        {
            try
            {
                Patient? patient = await _dbContext.Patients
                    .Include(p => p.Ordonnances)
                        .ThenInclude(o => o.Medicaments)
                    .Include(p => p.Allergies)
                    .Include(p => p.Antecedents)
                    .FirstOrDefaultAsync(p => p.PatientId == id);

                if (patient == null)
                {
                    return NotFound();
                }

                return View(patient);
            }
            catch (DbException ex)
            {
                _logger.LogError(ex, "Une erreur s'est produite lors de la récupération des détails du patient.");
                return RedirectToAction("Index", "Error");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Une erreur inattendue est survenue lors de la récupération des détails du patient.");
                return RedirectToAction("Index", "Error");
            }
        }


        public async Task<IActionResult> ObtenirPhoto(int id)
        {
            try
            {
                var patient = await _dbContext.Patients.FindAsync(id);
                if (patient == null || patient.Photo == null)
                {
                    return NotFound();
                }

                return File(patient.Photo, "image/jpeg");
            }
            catch (DbException ex)
            {
                _logger.LogError(ex, "Une erreur s'est produite lors de la récupération de la photo du patient.");
                return RedirectToAction("Index", "Error");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Une erreur inattendue est survenue lors de la récupération de la photo du patient.");
                return RedirectToAction("Index", "Error");
            }
        }

    }
}

