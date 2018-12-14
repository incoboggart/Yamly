// Copyright (c) 2018 Alexander Bogomoletz
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

using System.CodeDom;
using System.CodeDom.Compiler;
using System.Linq;
using System.Text;

using Microsoft.CSharp;

using Yamly.UnityEditor;

namespace Yamly.CodeGeneration
{
    internal sealed class UtilityAssemblyBuilder
    {
        private readonly CompilerParameters _compilerParameters = new CompilerParameters();
        private readonly string[] _groups;

        public UtilityAssemblyBuilder(string[] groups)
        {
            _groups = groups;
        }

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

        public string TargetNamespace { get; set; }

        public CompilerResults CompilerResults { get; private set; }

        public UtilityAssemblyBuilder Build()
        {
            _compilerParameters.GenerateExecutable = false;
            _compilerParameters.GenerateInMemory = false;
            _compilerParameters.ReferencedAssemblies.Add(typeof(YamlyAssetPostprocessor).Assembly.Location.ToUnityPath());
            _compilerParameters.ReferencedAssemblies.Add(CodeGenerationUtility.GetUnityEngineAssemblyPath());
            _compilerParameters.ReferencedAssemblies.Add(CodeGenerationUtility.GetUnityEditorAssemblyPath());

            var codeProvider = new CSharpCodeProvider();
            CompilerResults = codeProvider.CompileAssemblyFromDom(_compilerParameters, 
                GenerateValidateMethods(),
                GenerateRebuildMethods());
            
            return this;
        }

        private CodeSnippetCompileUnit GenerateValidateMethods()
        {
            var sourceCode = new StringBuilder();
            sourceCode.AppendLine("using UnityEditor;");
            sourceCode.AppendLine("using Yamly.UnityEditor;");
            sourceCode.AppendLine($"namespace {TargetNamespace}");
            sourceCode.AppendLine("{");
            sourceCode.AppendLine("    public static class ValidateYamlyGeneratedUtility");
            sourceCode.AppendLine("{");
            foreach (var group in _groups.Where(CodeGenerationUtility.IsValidGroupName))
            {
                var groupName = CodeGenerationUtility.GetGroupName(group);
                sourceCode.AppendLine($"[MenuItem(\"Yamly/Validate/{group}\")]");
                sourceCode.AppendLine($"public static void Validate{groupName}()");
                sourceCode.AppendLine("{");
                sourceCode.AppendLine($"YamlyAssetPostprocessor.Validate(\"{group}\");");
                sourceCode.AppendLine("}");
            }

            sourceCode.AppendLine("}");
            sourceCode.AppendLine("}");
            
            return new CodeSnippetCompileUnit(sourceCode.ToString());
        }

        private CodeSnippetCompileUnit GenerateRebuildMethods()
        {
            var sourceCode = new StringBuilder();
            sourceCode.AppendLine("using UnityEditor;");
            sourceCode.AppendLine("using Yamly.UnityEditor;");
            sourceCode.AppendLine($"namespace {TargetNamespace}");
            sourceCode.AppendLine("{");
            sourceCode.AppendLine("    public static class RebuildYamlyGeneratedUtility");
            sourceCode.AppendLine("{");
            foreach (var group in _groups.Where(CodeGenerationUtility.IsValidGroupName))
            {
                var groupName = CodeGenerationUtility.GetGroupName(group);
                sourceCode.AppendLine($"[MenuItem(\"Yamly/Rebuild/{group}\")]");
                sourceCode.AppendLine($"public static void Rebuild{groupName}()");
                sourceCode.AppendLine("{");
                sourceCode.AppendLine($"YamlyAssetPostprocessor.Rebuild(\"{group}\");");
                sourceCode.AppendLine("}");
            }

            sourceCode.AppendLine("}");
            sourceCode.AppendLine("}");
            
            return new CodeSnippetCompileUnit(sourceCode.ToString());
        }
    }
}
