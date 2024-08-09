using System.ComponentModel.DataAnnotations;

namespace Lib;

public static class ValidationExtensions
{
    public static bool IsValid(this object obj)
    {
        return Validator.TryValidateObject(obj, new ValidationContext(obj), null);
    }
}