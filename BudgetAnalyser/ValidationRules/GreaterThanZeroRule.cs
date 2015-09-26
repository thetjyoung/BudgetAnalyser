﻿using System.Globalization;
using System.Windows.Controls;

namespace BudgetAnalyser.ValidationRules
{
    public class GreaterThanZeroRule : ValidationRule
    {
        /// <summary>
        /// When overridden in a derived class, performs validation checks on a value.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Windows.Controls.ValidationResult"/> object.
        /// </returns>
        /// <param name="value">The value from the binding target to check.</param><param name="cultureInfo">The culture to use in this rule.</param>
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value != null)
            {
                double i;
                if (double.TryParse(value.ToString(), out i))
                {
                    if (i > 0) return new ValidationResult(true, null);
                }
            }

            return new ValidationResult(false, "Please enter a valid numeric value greater than zero.");
        }
    }
}
