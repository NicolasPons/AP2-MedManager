﻿using Microsoft.AspNetCore.Mvc;
using MedManager.Data;
using MedManager.Models;
using Microsoft.EntityFrameworkCore;
using MedManager.ViewModel.ContreIndication;
using System.Data.Common;
using Microsoft.AspNetCore.Authorization;

namespace MedManager.Controllers
{
    [Authorize]
    public class ContreIndicationsController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<Patient> _logger;

        public ContreIndicationsController(ApplicationDbContext dbContext, ILogger<Patient> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string filtreAllergies, string filtreAntecedents, string OrdreTriAllergie, string OrdreTriAntecedent)
        {
            List<Allergie> allergies = await _dbContext.Allergies.ToListAsync();
            List<Antecedent> antecedents = await _dbContext.Antecedents.ToListAsync();

			OrdreTriAllergie ??= "";
			OrdreTriAntecedent ??= "";
            ViewData["NomAllergieParamTri"] = string.IsNullOrEmpty(OrdreTriAllergie) ? "nomAllergie_desc" : "";
            ViewData["NomAntecedentParamTri"] = string.IsNullOrEmpty(OrdreTriAntecedent) ? "nomAntecedent_desc" : "";

			if (!string.IsNullOrEmpty(filtreAllergies))
            {
                allergies = allergies.Where(a => a.Nom.ToUpper().Contains(filtreAllergies.ToUpper())).ToList();
            }

            if (!string.IsNullOrEmpty(filtreAntecedents))
            {
                antecedents = antecedents.Where(a => a.Nom.ToUpper().Contains(filtreAntecedents.ToUpper())).ToList();
            }

            switch (OrdreTriAllergie)
            {
                case "nomAllergie_desc":
                    allergies = allergies.OrderByDescending(a => a.Nom).ToList();
					break;
                default:
                    allergies = allergies.OrderBy(a => a.Nom).ToList();
                    break;

			}

			switch (OrdreTriAntecedent)
			{
				case "nomAntecedent_desc":
					antecedents = antecedents.OrderByDescending(a => a.Nom).ToList();
					break;
				default:
					antecedents = antecedents.OrderBy(a => a.Nom).ToList();
					break;

			}


			var modele = (Allergies: allergies, Antecedents: antecedents);
            ViewData["TriActuelAllergie"] = OrdreTriAllergie;
			ViewData["TriActuelAntecedent"] = OrdreTriAntecedent;
			ViewData["FiltreActuelAllergie"] = filtreAllergies;
            ViewData["FiltreActuelAntecedent"] = filtreAntecedents;

            return View(modele);
        }

        public async Task<IActionResult> Ajouter(string type)
        {
            List<Medicament> medicaments = await _dbContext.Medicaments.ToListAsync();
            var model = new ContreIndicationViewModel
            {
                Medicaments = medicaments,
                Type = type
            };
            return View("Action", model);
        }

        [HttpPost]
        public async Task<IActionResult> Ajouter(ContreIndicationViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (model.Type == "Allergie")
                    {
                        Allergie allergie = new Allergie
                        {
                            Nom = model.Nom
                        };

                        if (model.IdMedicamentsSelectionnes != null)
                        {
                            var medicamentsSelectionnes = await ObtenirMedicamentSelectionnes(model.IdMedicamentsSelectionnes);
                            foreach (var medicament in medicamentsSelectionnes)
                            {
                                allergie.Medicaments.Add(medicament);
                            }
                        }

                        await _dbContext.Allergies.AddAsync(allergie);
                    }
                    else if (model.Type == "Antecedent")
                    {
                        Antecedent antecedent = new Antecedent
                        {
                            Nom = model.Nom
                        };

                        if (model.IdMedicamentsSelectionnes != null)
                        {
                            var medicamentsSelectionnes = await ObtenirMedicamentSelectionnes(model.IdMedicamentsSelectionnes);
                            foreach (var medicament in medicamentsSelectionnes)
                            {
                                antecedent.Medicaments.Add(medicament);
                            }
                        }
                        await _dbContext.Antecedents.AddAsync(antecedent);
                    }
                    await _dbContext.SaveChangesAsync();
                    TempData["MessageSucces"] = $"L'{model.Type.ToLower()} a été ajoutée avec succès.";
                    return RedirectToAction("Index");
                }

				List<Medicament> medicaments = await _dbContext.Medicaments.ToListAsync();
                var modelView = new ContreIndicationViewModel
                {
                    Medicaments = medicaments,
                    Type = model.Type
                };
				return View("Action", modelView);

			}
            catch (DbException ex)
            {
                _logger.LogError(ex, "Une erreur est apparue pendant l'ajout de la contre-indication'.");
                return RedirectToAction("Error");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Une erreur innatendue est survenue.");
                return RedirectToAction("Error");
            }
        }

        public async Task<IActionResult> Modifier(int id, string type)
        {
            try
            {

                List<Medicament> medicaments = await _dbContext.Medicaments.ToListAsync();
                if (type == "Allergie")
                {
                    var allergie = await _dbContext.Allergies
                        .Include(a => a.Medicaments)
                        .FirstOrDefaultAsync(a => a.AllergieId == id);
                    if (allergie == null)
                        return NotFound();
                    var viewModel = new ContreIndicationViewModel
                    {
                        Id = allergie.AllergieId,
                        Nom = allergie.Nom,
                        Type = "Allergie",
                        Medicaments = medicaments,
                        IdMedicamentsSelectionnes = allergie.Medicaments.Select(m => m.MedicamentId).ToList() ?? new List<int>()
                    };
                    return View("Action", viewModel);
                }
                else if (type == "Antecedent")
                {
                    var antecedent = await _dbContext.Antecedents
                            .Include(a => a.Medicaments)
                            .FirstOrDefaultAsync(a => a.AntecedentId == id);
                    if (antecedent == null)
                        return NotFound();
                    var viewModel = new ContreIndicationViewModel
                    {
                        Id = antecedent.AntecedentId,
                        Nom = antecedent.Nom,
                        Type = "Antecedent",
                        Medicaments = medicaments,
                        IdMedicamentsSelectionnes = antecedent.Medicaments.Select(m => m.MedicamentId).ToList() ?? new List<int>()
                    };
                    return View("Action", viewModel);
                }
                return NotFound();
            }
            catch (DbException ex)
            {
                _logger.LogError(ex, "Une erreur est apparue pendant la modification de la contre-indication.");
                return RedirectToAction("Error");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Une erreur innatendue est survenue.");
                return RedirectToAction("Error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Modifier(ContreIndicationViewModel model)
        {
            try
            {

                if (ModelState.IsValid)
                {
                    if (model.Type == "Allergie")
                    {
                        var allergie = await _dbContext.Allergies
                                .Include(a => a.Medicaments)
                                .FirstOrDefaultAsync(a => a.AllergieId == model.Id);

                        if (allergie == null)
                            return NotFound();
                        allergie.Nom = model.Nom;

                        allergie.Medicaments.Clear();
                        if (model.IdMedicamentsSelectionnes != null)
                        {
                            var medicamentsSelectionnes = await ObtenirMedicamentSelectionnes(model.IdMedicamentsSelectionnes);
                            foreach (var medicament in medicamentsSelectionnes)
                            {
                                allergie.Medicaments.Add(medicament);
                            }
                        }
                        _dbContext.Entry(allergie).State = EntityState.Modified;
                        await _dbContext.SaveChangesAsync();
                    }
                    else if (model.Type == "Antecedent")
                    {
                        var antecedent = await _dbContext.Antecedents
                            .Include(a => a.Medicaments)
                            .FirstOrDefaultAsync(a => a.AntecedentId == model.Id);
                        if (antecedent == null)
                            return NotFound();
                        antecedent.Nom = model.Nom;

                        antecedent.Medicaments.Clear();
                        if (model.IdMedicamentsSelectionnes != null)
                        {
                            var medicamentsSelectionnes = await ObtenirMedicamentSelectionnes(model.IdMedicamentsSelectionnes);

                            foreach (var medicament in medicamentsSelectionnes)
                            {
                                antecedent.Medicaments.Add(medicament);
                            }
                        }
                        _dbContext.Entry(antecedent).State = EntityState.Modified;
                        await _dbContext.SaveChangesAsync();
                    }
                    TempData["MessageSucces"] = $"L'{model.Type.ToLower()} a été modifié avec succès.";
                    return RedirectToAction("Index");
                }
                return View("Action", model);
            }
            catch (DbException ex)
            {
                _logger.LogError(ex, "Une erreur est apparue pendant la modification de la contre-indication.");
                return RedirectToAction("Error");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Une erreur innatendue est survenue.");
                return RedirectToAction("Error");
            }
        }

        public async Task<IActionResult> Supprimer(int id, string type)
        {
            try
            {
                if (type == "Allergie")
                {
                    var allergie = await _dbContext.Allergies.FindAsync(id);
                    if (allergie == null)
                        return NotFound();

                    _dbContext.Allergies.Remove(allergie);
                }
                else if (type == "Antecedent")
                {
                    var antecedent = await _dbContext.Antecedents.FindAsync(id);
                    if (antecedent == null)
                        return NotFound();

                    _dbContext.Antecedents.Remove(antecedent);
                }
                await _dbContext.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbException ex)
            {
                _logger.LogError(ex, $"Erreur de suppression pour {type} avec ID {id}.");
                TempData["MessageErreur"] = "Erreur de suppression. Veuillez réessayer.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur inattendue lors de la suppression.");
                TempData["MessageErreur"] = "Une erreur inattendue est survenue.";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Detail(int id, string type)
        {
            try
            {
                if(type == "Allergie")
                {
                    var allergie = await _dbContext.Allergies
                                    .Include(a => a.Medicaments)
                                    .FirstOrDefaultAsync(a => a.AllergieId == id);

                    if (allergie == null)
                        return NotFound();

                    var model = new ContreIndicationViewModel
                    {
                        Id = id,
                        Nom = allergie.Nom,
                        Medicaments = allergie.Medicaments, 
                        Type = type
                    };
                    return View(model);
                }
                else
                {
                    {
                        var antecedent = await _dbContext.Antecedents
                                        .Include(a => a.Medicaments)
                                        .FirstOrDefaultAsync(a => a.AntecedentId == id);

                        if (antecedent == null)
                            return NotFound();

                        var model = new ContreIndicationViewModel
                        {
                            Id = id,
                            Nom = antecedent.Nom,
                            Medicaments = antecedent.Medicaments,
                            Type = type
                        };
                        return View(model);
                    }
                }
            }
            catch (DbException ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des données.");
                TempData["MessageErreur"] = "Erreur de suppression. Veuillez réessayer.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur inattendue lors de la récupération des données.");
                TempData["MessageErreur"] = "Une erreur inattendue est survenue.";
                return RedirectToAction(nameof(Index));
            }

        }

        private async Task<List<Medicament>> ObtenirMedicamentSelectionnes(List<int> ids)
        {
            return await _dbContext.Medicaments
                .Where(m => ids.Contains(m.MedicamentId))
                .ToListAsync();
        }


    }
}
