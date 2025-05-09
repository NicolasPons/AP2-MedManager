﻿using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace MedManager.Attributs
{
    public class RequiredListeNonVideAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            var list = value as IList<int>;
            if (list != null && list.Count > 0)
            {
                return ValidationResult.Success!;
            }
            return new ValidationResult(ErrorMessage ?? "Veuillez sélectionner au moins un médicament");
        }
    }
}
