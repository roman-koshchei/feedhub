using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib;

public static class ValidationExtensions
{
    public static bool IsValid(this object obj)
    {
        return Validator.TryValidateObject(obj, new ValidationContext(obj), null);
    }
}