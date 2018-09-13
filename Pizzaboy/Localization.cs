using System.Collections.Generic;

namespace Pizzaboy
{
    public class Localization
    {
        public static Dictionary<string, string> Strings = new Dictionary<string, string>();

        public static string Get(string key, params object[] args)
        {
            if (Strings.ContainsKey(key))
            {
                return args.Length > 0 ? string.Format(Strings[key], args) : Strings[key];
            }
            else
            {
                return $"UNKNOWN KEY {key}";
            }
        }
    }
}
