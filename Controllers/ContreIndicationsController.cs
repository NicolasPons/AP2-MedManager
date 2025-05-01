using Microsoft.AspNetCore.Mvc;
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
        private readonly ILogger<ContreIndicationsController> _logger;

        public ContreIndicationsController(ApplicationDbContext dbContext, ILogger<ContreIndicationsController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                List<Allergie> allergies = await _dbContext.Allergies.ToListAsync();
                List<Antecedent> antecedents = await _dbContext.Antecedents.ToListAsync();
                var modele = (Allergies: allergies, Antecedents: antecedents);

                return View(modele);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'affichage des allergies et antécédents.");
                return RedirectToAction("Index", "Error");
            }
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
						TempData["SuccessMessage"] = "L'allergie a été ajoutée avec succès.";
						
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
						TempData["SuccessMessage"] = "L'antécédent a été ajouté avec succès.";
                    }
                    await _dbContext.SaveChangesAsync();
	
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
                return RedirectToAction("Index", "Error");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Une erreur innatendue est survenue.");
                return RedirectToAction("Index", "Error");
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
                return RedirectToAction("Index", "Error");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Une erreur innatendue est survenue.");
                return RedirectToAction("Index", "Error");
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
						TempData["SuccessMessage"] = "L'allergie a été modifiée avec succès.";
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
						TempData["SuccessMessage"] = "L'antécédent a été modifié avec succès.";
                    }
         
					return RedirectToAction("Index");
                }
                return View("Action", model);
            }
            catch (DbException ex)
            {
                _logger.LogError(ex, "Une erreur est apparue pendant la modification de la contre-indication.");
                return RedirectToAction("Index", "Error");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Une erreur innatendue est survenue.");
                return RedirectToAction("Index", "Error");
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
					TempData["SuccessMessage"] = "L'allergie a été supprimée avec succès.";
				}
                else if (type == "Antecedent")
                {
                    var antecedent = await _dbContext.Antecedents.FindAsync(id);
                    if (antecedent == null)
                        return NotFound();

                    _dbContext.Antecedents.Remove(antecedent);
					TempData["SuccessMessage"] = "L'antécédent a été supprimé avec succès.";
				}
                await _dbContext.SaveChangesAsync();
				return RedirectToAction(nameof(Index));
            }
            catch (DbException ex)
            {
                _logger.LogError(ex, $"Erreur de suppression pour {type} avec ID {id}.");
                TempData["MessageErreur"] = "Erreur de suppression. Veuillez réessayer.";
                return RedirectToAction("Index", "Error");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur inattendue lors de la suppression.");
                TempData["MessageErreur"] = "Une erreur inattendue est survenue.";
                return RedirectToAction("Index", "Error");
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
                return RedirectToAction("Index", "Error");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur inattendue lors de la récupération des données.");
                TempData["MessageErreur"] = "Une erreur inattendue est survenue.";
                return RedirectToAction("Index", "Error");
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
