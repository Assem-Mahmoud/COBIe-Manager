using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace COBIeManager.Shared.Validators
{
    public class PositiveNumberValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (double.TryParse(value?.ToString(), out double result))
            {
                return result > 0
                    ? ValidationResult.ValidResult
                    : new ValidationResult(false, "Value must be greater than zero.");
            }

            return new ValidationResult(false, "Invalid number.");
        }
    }

}
