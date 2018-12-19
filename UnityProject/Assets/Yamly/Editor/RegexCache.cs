using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Yamly
{
    public class RegexCache
    {
        private const string NamespacePatternBase = @"((namespace){1}){1}[\s\S]+NamespaceName{1}(?![a-zA-Z\d])(?=[\s\S]+|{)";
        private const string ClassPatternBase = @"((internal|public|private|protected|sealed|abstract|static)?[\s\r\n\t]+){0,2}(class|struct){1}[\s\S]+ClassName{1}(?![a-zA-Z\d])(?=[\s\S]+|{)";
        
        private readonly Dictionary<string, Regex> _namespace = new Dictionary<string, Regex>();
        private readonly Dictionary<string, Regex> _class = new Dictionary<string, Regex>();

        public Regex GetNamespaceRegex(string @namespace)
        {
            Regex regex;
            if (!_namespace.TryGetValue(@namespace, out regex))
            {
                regex = new Regex(NamespacePatternBase.Replace("NamespaceName", @namespace));

                _namespace[@namespace] = regex;
            }

            return regex;
        }

        public Regex GetTypeRegex(Type type)
        {
            return GetTypeRegex(type.Name);
        }

        public Regex GetTypeRegex(string type)
        {
            Regex regex;
            if (!_class.TryGetValue(type, out regex))
            {
                regex = new Regex(ClassPatternBase.Replace("ClassName", type));

                _class[type] = regex;
            }

            return regex;
        }
    }
}