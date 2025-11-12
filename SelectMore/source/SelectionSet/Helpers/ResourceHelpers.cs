using System.Globalization;
using System.Reflection;
using System.Resources;


namespace SelectionSet.Helpers
{
    internal static class ResourceHelpers
    {
        public static bool TryGetString(string key, CultureInfo culture, out string? value)
        {
            value = null;
            ResourceManager rm = new ResourceManager(
                "SelectionSet.Resources",
                Assembly.GetExecutingAssembly());
            value = rm.GetString(key, culture);
            return value != null;
        }

        public static bool TryGetCurrentLocalisation(string key, out string? value)
        {
            value = null;
            try
            {
                ResourceManager rm = new ResourceManager(
                    "SelectionSet.Resources",
                    Assembly.GetExecutingAssembly());
                value = rm.GetString(key, CultureInfo.CurrentCulture);
            }
            catch
            {

            }
            return value != null;
        }
    }
}
