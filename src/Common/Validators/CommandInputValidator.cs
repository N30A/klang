namespace Klang.Common.Validators;

public static class CommandInputValidator
{
    public static bool TrySanitizeQuery(string input, out string cleaned, out string error)
    {   
        error = string.Empty;
        cleaned = input.Replace("\"", string.Empty).Replace("\\", string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(cleaned))
        {
            error = "No query was provided.";
            return false;
        }
        
        return true;
    }
}