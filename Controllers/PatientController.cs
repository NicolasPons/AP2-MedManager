﻿using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MedManager.Models;
using Microsoft.AspNetCore.Identity;
using MedManager.Data;
using Microsoft.EntityFrameworkCore;
using MedManager.ViewModel.PatientVM;

namespace MedManager.Controllers
{

    public class PatientController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        public PatientController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> Ajouter(string id)
        {
            var viewModel = new AjouterPatientViewModel
            {
                Allergies = await _dbContext.Allergies.ToListAsync(),
                Antecedents = await _dbContext.Antecedents.ToListAsync(),
                IdMedecin = id,
            };
            return View(viewModel);
        }

        public async Task<IActionResult> Ajouter(AjouterPatientViewModel model)
        {

            Medecin? medecin = _dbContext.Users.FirstOrDefault(m => m.Id == model.IdMedecin);

            if (ModelState.IsValid)
            {
                var patient = new Patient
                {
                    Nom = model.Patient.Nom,
                    Prenom = model.Patient.Prenom,
                    NuméroSécuritéSociale = model.Patient.NuméroSécuritéSociale,
                    DateNaissance = model.Patient.DateNaissance,
                    Adresse = model.Patient.Adresse,
                    Ville = model.Patient.Ville,
                    Sexe = model.Patient.Sexe,
                    MedecinID = model.IdMedecin,
                    medecin = medecin, 
                };

                if (model.SelectedAllergieIds != null)
                {
                    var selectedAllergies = await _dbContext.Allergies
                        .Where(a => model.SelectedAllergieIds.Contains(a.AllergieId))
                        .ToListAsync();
                    foreach (var allergie in selectedAllergies)
                    {
                        patient.Allergies.Add(allergie);
                    }
                }

                if (model.SelectedAntecedentIds != null)
                {
                    var selectedAntecedents = await _dbContext.Antecedents
                        .Where(a => model.SelectedAntecedentIds.Contains(a.AntecedentId))
                        .ToListAsync();
                    foreach (var antecedent in selectedAntecedents)
                    {
                        patient.Antecedents.Add(antecedent);
                    }
                }

                _dbContext.Patients.AddAsync(patient);
                await _dbContext.SaveChangesAsync();
                return RedirectToAction("Index", "Medcin");

            };
            return View(model);
        }
    }

}

