using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace lgpg
{
    public static class utilities
    {
        public static string[] split(string split, string toSplit)
        {
            return toSplit.Split(new string[] { split }, StringSplitOptions.None);
        }
    }
}
