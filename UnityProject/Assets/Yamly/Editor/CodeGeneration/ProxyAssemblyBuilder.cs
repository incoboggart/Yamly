﻿// Copyright (c) 2018 Alexander Bogomoletz
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using Microsoft.CSharp;

using UnityEngine;

using Yamly.Proxy;
using Yamly.UnityEditor;

namespace Yamly.CodeGeneration
{
    internal sealed class ProxyAssemblyBuilder
    {
        private readonly ProxyCodeGenerator _proxyCodeGenerator = new ProxyCodeGenerator(Context.Roots);
        private readonly CompilerParameters _compilerParameters = new CompilerParameters();

        public string TargetNamespace { get; set; } = "Yamly.Generated";

        public string OutputAssembly
        {
            get { return _compilerParameters.OutputAssembly;}
            set { _compilerParameters.OutputAssembly = value; }
        }

        public bool IncludeDebugInformation
        {
            get { return _compilerParameters.IncludeDebugInformation; }
            set { _compilerParameters.IncludeDebugInformation = value; }
        }

        public bool TreatWarningsAsErrors
        {
            get { return _compilerParameters.TreatWarningsAsErrors; }
            set { _compilerParameters.TreatWarningsAsErrors = value; }
        }

        public CompilerResults CompilerResults { get; private set; }
        
        private readonly List<Type> _ignoreAttributeTypes = new List<Type>();
        private readonly List<string> _ignoredAttributeNames = new List<string>();

        public ProxyAssemblyBuilder Ignore<T>()
            where T : Attribute
        {
            var t = typeof(T);
            if (!_ignoreAttributeTypes.Contains(t))
            {
                _ignoreAttributeTypes.Add(t);
            }

            return this;
        }

        public ProxyAssemblyBuilder IgnoreAttribute(string className)
        {
            if (!_ignoredAttributeNames.Contains(className))
            {
                _ignoredAttributeNames.Add(className);
            }

            return this;
        }

        public ProxyAssemblyBuilder Build()
        {
            Ignore<IgnoreAttribute>();

            _proxyCodeGenerator.StorageOutputNamespace = $"{TargetNamespace}.Storages";
            _proxyCodeGenerator.ProxyOutputNamespace = $"{TargetNamespace}.Proxy";

            var proxyCompileUnit = GenerateProxy();
            
            var referencedAssemblies = new List<string>();
            referencedAssemblies.AddRange(_proxyCodeGenerator.ReferencedAssemblies.Select(a => a.Location.ToUnityPath()).ToArray());
            referencedAssemblies.Add(typeof(ProxyUtility).Assembly.Location.ToUnityPath());
            referencedAssemblies.Add(typeof(AssetDeclarationAttributeBase).Assembly.Location.ToUnityPath());
            referencedAssemblies.Add(typeof(ISerializationCallbackReceiver).Assembly.Location.ToUnityPath());
            referencedAssemblies.Add(typeof(ScriptableObject).Assembly.Location.ToUnityPath());
            referencedAssemblies.Add(typeof(JsonUtility).Assembly.Location.ToUnityPath());
            
            _compilerParameters.GenerateExecutable = false;
            _compilerParameters.GenerateInMemory = false;
            _compilerParameters.ReferencedAssemblies.AddRange(referencedAssemblies.Distinct().ToArray());
            
            var assemblyAttributeUnit = new CodeCompileUnit();
            assemblyAttributeUnit.AssemblyCustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(YamlyProxyAssemblyAttribute))));

            var codeProvider = new CSharpCodeProvider();

            CompilerResults = codeProvider.CompileAssemblyFromDom(_compilerParameters, 
                proxyCompileUnit, 
                GenerateAssemblyAttribute(), 
                GenerateEnum(),
                GenerateStorageExtensionMethods(),
                GenerateJsonProxyExtensionMethods());

            return this;
        }

        private CodeCompileUnit GenerateProxy()
        {
            var sourceCode = _proxyCodeGenerator.GenerateProxySourceCode();

            if (YamlySettings.Instance.VerboseLogs)
            {
                foreach (var log in _proxyCodeGenerator.Logs)
                {
                    LogUtils.Verbose(log);
                }
            }

            return new CodeSnippetCompileUnit(sourceCode);
        }

        private CodeCompileUnit GenerateAssemblyAttribute()
        {
            var assemblyAttributeUnit = new CodeCompileUnit();
            assemblyAttributeUnit.AssemblyCustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(YamlyProxyAssemblyAttribute))));
            return assemblyAttributeUnit;
        }

        private CodeCompileUnit GenerateEnum()
        {
            var sourceCode = new StringBuilder();

            var groups = Context.Groups;

            sourceCode.AppendLine($"namespace {TargetNamespace}");
            sourceCode.AppendLine("{");
            sourceCode.AppendLine("public enum Assets");
            sourceCode.AppendLine("{");
            for (var i = 0; i < groups.Count; i++)
            {
                sourceCode.Append(CodeGenerationUtility.GetGroupName(groups[i])).AppendLine(",");
            }

            sourceCode.AppendLine("}");

            sourceCode.AppendLine("public static class AssetsExtensions");
            sourceCode.AppendLine("{");
            sourceCode.AppendLine("public static string ToGroupName(this Assets assets)");
            sourceCode.AppendLine("{");
            sourceCode.AppendLine("switch(assets)");
            sourceCode.AppendLine("{");
            foreach (var group in groups)
            {
                var groupName = CodeGenerationUtility.GetGroupName(group);
                sourceCode.AppendLine($"case Assets.{groupName}:");
                sourceCode.AppendLine($"return \"{group}\";");
            }

            sourceCode.AppendLine("default: throw new System.NotSupportedException(assets.ToString());");
            sourceCode.AppendLine("}");
            sourceCode.AppendLine("}");
            sourceCode.AppendLine("}");

            sourceCode.AppendLine("}");
            
            return new CodeSnippetCompileUnit(sourceCode.ToString());
        }

        private CodeCompileUnit GenerateStorageExtensionMethods()
        {
            var sourceCode = new StringBuilder();           

            var g = new Dictionary<string, Type>();
            foreach (var definition in Context.Roots)
            {
                foreach (var attribute in definition.ValidAttributes)
                {
                    switch (attribute.GetDeclarationType())
                    {
                        case DeclarationType.Single:
                            g[attribute.Group] = definition.Root;
                            break;
                        case DeclarationType.List:
                            var elementType = definition.Root;
                            g[attribute.Group] = typeof(List<>).MakeGenericType(elementType);
                            break;
                        case DeclarationType.Dictionary:
                            var dictionaryAttribute = (AssetDictionaryAttribute) attribute;
                            if (dictionaryAttribute.IsSingleFile)
                            {
                                g[attribute.Group] = dictionaryAttribute.KeyType;
                            }
                            
                            var keySourceMethodInfo = definition.Root.GetKeySourceMethodInfo(dictionaryAttribute);
                            if (keySourceMethodInfo == null)
                            {
                                continue;
                            }

                            var keyType = keySourceMethodInfo.ReturnType;
                            var valueType = definition.Root;
                            g[attribute.Group] = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            sourceCode.AppendLine($"namespace {TargetNamespace}");
            sourceCode.AppendLine("{");
            sourceCode.AppendLine("using System.Collections.Generic;");
            sourceCode.AppendLine("public static class StorageDefinitionExtensions");
            sourceCode.AppendLine("{");

            sourceCode.AppendLine($"public static T Get<T>(this {nameof(Storage)} s, {TargetNamespace}.Assets asset)");
            sourceCode.AppendLine("{");
            sourceCode.AppendLine("return s.Get<T>(asset.ToGroupName());");
            sourceCode.AppendLine("}");

            sourceCode.AppendLine($"public static bool Contains(this {nameof(Storage)} s, {TargetNamespace}.Assets asset)");
            sourceCode.AppendLine("{");
            sourceCode.AppendLine("return s.Contains(asset.ToGroupName());");
            sourceCode.AppendLine("}");

            sourceCode.AppendLine($"public static bool Contains<T>(this {nameof(Storage)} s, {TargetNamespace}.Assets asset)");
            sourceCode.AppendLine("{");
            sourceCode.AppendLine("return s.Contains<T>(asset.ToGroupName());");
            sourceCode.AppendLine("}");

            var groups = Context.Groups;
            for (var i = 0; i < groups.Count; i++)
            {
                var group = groups[i];
                if (!g.ContainsKey(group))
                {
                    continue;
                }
                
                var groupName = CodeGenerationUtility.GetGroupName(group);
                var typeName = _proxyCodeGenerator.GetTypeName(g[group]);
                sourceCode.AppendLine($"public static {typeName} Get{groupName}(this Storage s)");
                sourceCode.AppendLine("{");
                sourceCode.AppendLine($"return s.Get<{typeName}>(\"{group}\");");
                sourceCode.AppendLine("}");
            }

            sourceCode.AppendLine("}");
            sourceCode.AppendLine("}");

            return new CodeSnippetCompileUnit(sourceCode.ToString());
        }

        private CodeCompileUnit GenerateJsonProxyExtensionMethods()
        {
            var sourceCode = new StringBuilder();

            var groupType = new Dictionary<string, Type>();
            foreach (var definition in Context.Roots)
            {
                foreach (var attribute in definition.ValidAttributes)
                {
                    switch (attribute.GetDeclarationType())
                    {
                        case DeclarationType.Single:
                            groupType[attribute.Group] = definition.Root;
                            break;
                        case DeclarationType.List:
                            var elementType = definition.Root;
                            groupType[attribute.Group] = typeof(List<>).MakeGenericType(elementType);
                            break;
                        case DeclarationType.Dictionary:
                            var dictionaryAttribute = (AssetDictionaryAttribute)attribute;
                            var keySourceMethodInfo = definition.Root.GetKeySourceMethodInfo(dictionaryAttribute);
                            if (keySourceMethodInfo == null)
                            {
                                continue;
                            }

                            var keyType = keySourceMethodInfo.ReturnType;
                            var valueType = definition.Root;
                            groupType[attribute.Group] = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            var listGenericType = typeof(List<>);
            var dictionaryGenericType = typeof(Dictionary<,>);
            
            var declaredInputTypes = new List<Type>();

            var namespaces = new List<string>();
            namespaces.Add(typeof(string).Namespace);
            namespaces.Add(typeof(List<>).Namespace);
            namespaces.Add(typeof(ProxyUtility).Namespace);
            namespaces.Add(_proxyCodeGenerator.ProxyOutputNamespace);
            namespaces.Add(_proxyCodeGenerator.StorageOutputNamespace);
            foreach (var root in Context.Roots)
            {
                if (root.Root.Namespace != null)
                {
                    namespaces.Add(root.Root.Namespace);
                }

                namespaces.Add(_proxyCodeGenerator.GetProxyNamespaceName(root.Root));
            }
           
            foreach (var ns in namespaces.Distinct())
            {
                sourceCode.AppendLine($"using {ns};");                
            }
            
            sourceCode.AppendLine($"namespace {TargetNamespace}");
            sourceCode.AppendLine("{");
            sourceCode.AppendLine("public static class JsonUtility");
            sourceCode.AppendLine("{");
            foreach (var root in Context.Roots)
            {
                foreach (var attribute in root.Attributes.Where(a => a.IsValid()))
                {
                    string inputTypeName = null;
                    string proxyTypeName = null;
                    string proxyToOrigin = null;
                    string originToProxy = null;
                    bool declareToMethod = false;
                    const string proxy = "proxy";
                    const string origin = "origin";
                    switch (attribute.GetDeclarationType())
                    {
                            case DeclarationType.Single:
                                var singleType = root.Root;
                                inputTypeName = _proxyCodeGenerator.GetTypeName(singleType);
                                proxyTypeName = _proxyCodeGenerator.GetTypeName(singleType, true);
                                proxyTypeName = _proxyCodeGenerator.GetGluedTypeName(proxyTypeName);
                                
                                proxyToOrigin = _proxyCodeGenerator.GetTypeConversion(singleType, proxy, false);
                                originToProxy = _proxyCodeGenerator.GetTypeConversion(singleType, origin, true);
                                
                                if (!declaredInputTypes.Contains(singleType))
                                {
                                    declaredInputTypes.Add(singleType);
                                    declareToMethod = true;
                                }
                                break;
                            case DeclarationType.List:
                                var elementType = root.Root;
                                var listType = listGenericType.MakeGenericType(elementType);
                                inputTypeName = _proxyCodeGenerator.GetTypeName(listType);
                                proxyTypeName = _proxyCodeGenerator.GetTypeName(listType, true);
                                
                                proxyToOrigin = _proxyCodeGenerator.GetTypeConversion(listType, proxy, false);
                                originToProxy = _proxyCodeGenerator.GetTypeConversion(listType, origin, true);
                                
                                if (!declaredInputTypes.Contains(elementType))
                                {
                                    declaredInputTypes.Add(elementType);
                                    declareToMethod = true;
                                }
                                break;
                            case DeclarationType.Dictionary:
                                var keyType = root.Root.GetDictionaryKeyType(attribute as AssetDictionaryAttribute);
                                var valueType = root.Root;
                                var dictionaryType = dictionaryGenericType.MakeGenericType(keyType, valueType);
                                inputTypeName = _proxyCodeGenerator.GetTypeName(dictionaryType);
                                proxyTypeName = _proxyCodeGenerator.GetTypeName(dictionaryType, true);
                                
                                proxyToOrigin = _proxyCodeGenerator.GetTypeConversion(dictionaryType, proxy, false);
                                originToProxy = _proxyCodeGenerator.GetTypeConversion(dictionaryType, origin, true);
                                
                                if (!declaredInputTypes.Contains(dictionaryType))
                                {
                                    declaredInputTypes.Add(dictionaryType);
                                    declareToMethod = true;
                                }
                                break;
                    } 
                    
                    LogUtils.Info($"{attribute.Group}: {inputTypeName} -> {proxyTypeName}");
                    if (declareToMethod)
                    {
                        sourceCode.AppendLine("/// <summary>");
                        sourceCode.AppendLine($"/// Serialize {attribute.Group} value to json");
                        sourceCode.AppendLine("/// </summary>");
                        sourceCode.AppendLine($"public static string ToJson(this {inputTypeName} origin, bool pretty)");
                        sourceCode.AppendLine("{");
                        sourceCode.AppendLine($"var proxy = {originToProxy};");
                        sourceCode.AppendLine("return UnityEngine.JsonUtility.ToJson(proxy, pretty);");
                        sourceCode.AppendLine("}");
                        sourceCode.AppendLine();
                    }

                    sourceCode.AppendLine("/// <summary>");
                    sourceCode.AppendLine($"/// Parse json to {attribute.Group} value");
                    sourceCode.AppendLine("/// </summary>");
                    sourceCode.AppendLine($"public static {inputTypeName} To{attribute.Group}(string json)");
                    sourceCode.AppendLine("{");
                    sourceCode.AppendLine($"var proxy = UnityEngine.JsonUtility.FromJson<{proxyTypeName}>(json);");
                    sourceCode.AppendLine($"return {proxyToOrigin};");
                    sourceCode.AppendLine("}");
                }
            }
            sourceCode.AppendLine("}");
            sourceCode.AppendLine("}");

            File.WriteAllText(@"C:\Users\0bogg\Desktop\test.cs", sourceCode.ToString());
            
            return new CodeSnippetCompileUnit(sourceCode.ToString());
        }
    }
}
