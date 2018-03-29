using System;
using System.Collections.Generic;
using System.Dynamic;

namespace IgiCore_Queue.Server.Extensions
{
    public static class ExpandoObjectExtentions
    {
        public static string Print(this ExpandoObject dynamicObject)
        {
            var dynamicDictionary = dynamicObject as IDictionary<string, object>;
            string output = "";
            foreach (KeyValuePair<string, object> property in dynamicDictionary)
            {
                output += $"{property.Key}: {property.Value.ToString()}" + Environment.NewLine;
            }

            return output;

        }
    }
}