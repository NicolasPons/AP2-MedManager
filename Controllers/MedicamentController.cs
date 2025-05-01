using MedManager.Models;
using MedManager.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using Microsoft.AspNetCore.Authorization;
using iText.Commons.Actions.Contexts;
using MedManager.ViewModel.MedicamentViewModel;
namespace MedManager.Controllers
{
    [Authorize]
    public class MedicamentController : Controller
	{
		private readonly ILogger<MedicamentController> _logger;
		private readonly ApplicationDbContext _dbContext;

		public MedicamentController(ILogger<MedicamentController> logger, ApplicationDbContext dbContext)
		{
			_dbContext = dbContext;
			_logger = logger;
		}

        public async Task<IActionResult> Index(MedicamentViewModel model)
        {
            try
            {
                List<Medicament> medicaments = await _dbContext.Medicaments.ToListAsync();

                var viewModel = new MedicamentViewModel
                {
                    Medicaments = medicaments
                };
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'affichage de la liste des médicaments");
                return RedirectToAction("Index", "Error");
            }
        }



        [HttpGet]
		public async Task<IActionResult> Modifier(int id)
		{

			Medicament? medicament = await _dbContext.Medicaments.FindAsync(id);
			return View(medicament);
		}

		[HttpPost]
		public async Task<IActionResult> Modifier(Medicament model)
		{

			try
			{
				if (ModelState.IsValid)
				{
					Medicament? medicament = await _dbContext.Medicaments.FindAsync(model.MedicamentId);

					if (medicament == null)
						return NotFound();

					medicament.Nom = model.Nom;
					medicament.Quantite = model.Quantite;
					medicament.Posologie = model.Posologie;
					medicament.Composition = model.Composition;
					medicament.Categorie = model.Categorie;
					_dbContext.Entry(medicament).State = EntityState.Modified;
					await _dbContext.SaveChangesAsync();
					TempData["SuccessMessage"] = "Le médicament a été modifié avec succès.";	
					return RedirectToAction("Index", "Medicament");
				}
				return View(model);
            }
			catch (DbException ex)
			{
				_logger.LogError(ex, "Une erreur est apparue pendant la modification du médicament.");
				return RedirectToAction("Index", "Error");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Une erreur innatendue est survenue.");
				return RedirectToAction("Index", "Error");
			}
		}

		public async Task<IActionResult> Supprimer(int id)
		{
			try
			{
				Medicament? medicament = await _dbContext.Medicaments.FindAsync(id);

				if (medicament == null)
					return NotFound();

                _dbContext.Remove(medicament);
                await _dbContext.SaveChangesAsync();
				TempData["SuccessMessage"] = "Le médicament a été supprimé avec succès.";
				return RedirectToAction("Index", "Medicament");
			}
			catch (DbException ex)
			{
				_logger.LogError(ex, "Une erreur est apparue pendant la suppression du médicament.");
				return RedirectToAction("Index", "Error");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Une erreur innatendue est survenue.");
				return RedirectToAction("Index", "Error");
			}
		}
		[HttpGet]
		public IActionResult Ajouter()
		{
			return View();
		}

		[HttpPost]
		public async Task<IActionResult> Ajouter(Medicament model)
		{
			try
			{
				if (ModelState.IsValid)
				{
					await _dbContext.Medicaments.AddAsync(model);
					await _dbContext.SaveChangesAsync();
					TempData["SuccessMessage"] = "Le médicament a été ajouté avec succès.";	
					return RedirectToAction("Index", "Medicament");
				}
				return View(model);

            }
			catch (DbException ex)
			{
				_logger.LogError(ex, "Une erreur est apparue pendant l'ajout du médicament.");
				return RedirectToAction("Index", "Error");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Une erreur innatendue est survenue.");
				return RedirectToAction("Index", "Error");
			}
		}

		public async Task<IActionResult> Detail(int id )
		{
			try
			{
				var medicament = await _dbContext.Medicaments
								.Include(m => m.Allergies)
								.Include(m => m.Antecedents)
								.FirstOrDefaultAsync(m => m.MedicamentId == id);
				return View(medicament);
			}
			catch (DbException ex)
			{
				_logger.LogError(ex, "Une erreur est apparue pendant la récupération des données du médicament.");
				return RedirectToAction("Index", "Error");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Une erreur innatendue est survenue.");
				return RedirectToAction("Index", "Error");
			}
		}
	}
}
