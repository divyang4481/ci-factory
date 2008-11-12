// NAnt - A .NET build tool
// Copyright (C) 2001-2002 Gerry Shaw
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//
// Sergey Chaban (serge@wildwestsoftware.com)
// Gerry Shaw (gerry_shaw@yahoo.com)
// Ian MacLean (ian at maclean.ms)
// Giuseppe Greco (giuseppe.greco@agamura.com)

using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;
using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Types;
using NAnt.Core.Util;
using NAnt.DotNet.Types;

namespace NAnt.DotNet.Tasks {
    /// <summary>
    /// Executes the code contained within the task. This code can include custom extension function definitions. 
    /// Once the script task has executed those custom functions will be available for use in the buildfile.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///     The <see cref="ScriptTask" /> must contain a single <c>code</c> 
    ///     element, which in turn contains the script code.
    ///     </para>
    ///     <para>
    ///     A static entry point named <c>ScriptMain</c> is required if no custom functions have been defined. It must 
    ///     have a single <see cref="Project"/> parameter.
    ///     </para>
    ///     <para>
    ///     The following namespaces are loaded by default:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>System</description>
    ///         </item>
    ///         <item>
    ///             <description>System.Collections</description>
    ///         </item>
    ///         <item>
    ///             <description>System.Collections.Specialized</description>
    ///         </item>
    ///         <item>
    ///             <description>System.IO</description>
    ///         </item>
    ///         <item>
    ///             <description>System.Text</description>
    ///         </item>
    ///         <item>
    ///             <description>System.Text.RegularExpressions</description>
    ///         </item>
    ///         <item>
    ///             <description>NAnt.Core</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <example>
    ///   <para>Run C# code that writes a message to the build log.</para>
    ///   <code>
    ///         &lt;script language=&quot;C#&quot;&gt;
    ///             &lt;code&gt;
    ///               &lt;![CDATA[
    ///                 public static void ScriptMain(Project project) {
    ///                     project.Log(Level.Info, &quot;Hello World from a script task using C#&quot;);
    ///                 }
    ///               ]]&gt;
    ///             &lt;/code&gt;
    ///         &lt;/script&gt;
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>Define a custom function and call it using C#.</para>
    ///   <code>
    ///         &lt;script language=&quot;C#&quot; prefix=&quot;test&quot; &gt;
    ///             &lt;code&gt;
    ///               &lt;![CDATA[
    ///                 [Function("test-func")]
    ///                 public static string Testfunc(  ) {
    ///                     return "some result !!!!!!!!";
    ///                 }
    ///               ]]&gt;
    ///             &lt;/code&gt;
    ///         &lt;/script&gt;
    ///         &lt;echo message='${test::test-func()}'/&gt;
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>Run Visual Basic.NET code that writes a message to the build log.</para>
    ///   <code>
    ///         &lt;script language=&quot;VB&quot;&gt;
    ///             &lt;code&gt;
    ///               &lt;![CDATA[
    ///                 Public Shared Sub ScriptMain(project As Project)
    ///                     project.Log(Level.Info, &quot;Hello World from a script task using Visual Basic.NET&quot;)
    ///                 End Sub
    ///               ]]&gt;
    ///             &lt;/code&gt;
    ///         &lt;/script&gt;
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>Define a custom task and call it using C#.</para>
    ///   <code>
    ///         &lt;script language=&quot;C#&quot; prefix=&quot;test&quot; &gt;
    ///             &lt;code&gt;
    ///               &lt;![CDATA[
    ///                 [TaskName("usertask")]
    ///                 public class TestTask : Task {
    ///                   #region Private Instance Fields
    ///
    ///                   private string _message;
    ///
    ///                   #endregion Private Instance Fields
    ///
    ///                   #region Public Instance Properties
    ///
    ///                   [TaskAttribute("message", Required=true)]
    ///                   public string FileName {
    ///                       get { return _message; }
    ///                       set { _message = value; }
    ///                   }
    ///
    ///                   #endregion Public Instance Properties
    ///
    ///                   #region Override implementation of Task
    ///
    ///                   protected override void ExecuteTask() {
    ///                       Log(Level.Info, _message.ToUpper());
    ///                   }
    ///                   #endregion Override implementation of Task
    ///                 }
    ///               ]]&gt;
    ///             &lt;/code&gt;
    ///         &lt;/script&gt;
    ///         &lt;usertask message='Hello from UserTask'/&gt;
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>Define a custom function and call it using Boo (http://boo.codehaus.org/).</para>
    ///   <code>
    ///         &lt;script language=&quot;Boo.CodeDom.BooCodeProvider, Boo.CodeDom, Version=1.0.0.0, Culture=neutral, PublicKeyToken=32c39770e9a21a67&quot;
    ///             failonerror=&quot;true&quot;&gt;
    ///             &lt;code&gt;
    ///               &lt;![CDATA[
    ///                
    ///                 [Function(&quot;test-func&quot;)]
    ///                 def MyFunc():
    ///                     return &quot;Hello from Boo !!!!!!&quot;
    ///               ]]&gt;
    ///             &lt;/code&gt;
    ///         &lt;/script&gt;
    ///         &lt;echo message='${script::test-func()}'/&gt;
    ///   </code>
    /// </example>
    [TaskName("script")]
    public class ScriptTask : Task {
        #region Private Instance Fields

        private string _language = null;
        private AssemblyFileSet _references = new AssemblyFileSet();
        private string _mainClass = "";
        private string _rootClassName;
        private string _prefix = "script";
        private NamespaceImportCollection _imports = new NamespaceImportCollection();
        private RawXml _code;

        #endregion Private Instance Fields

        #region Private Static Fields

        private static readonly string[] _defaultNamespaces = {
                                                                  "System",
                                                                  "System.Collections",
                                                                  "System.Collections.Specialized",
                                                                  "System.IO",
                                                                  "System.Text",
                                                                  "System.Text.RegularExpressions",
                                                                  "NAnt.Core",
                                                                  "NAnt.Core.Attributes"};

        #endregion Private Static Fields

        #region Public Instance Properties

        /// <summary>
        /// The language of the script block. Possible values are "VB", "vb", "VISUALBASIC", "C#", "c#", "CSHARP".
        /// "JS", "js", "JSCRIPT" "VJS", "vjs", "JSHARP" or a fully-qualified name for a class implementing 
        /// <see cref="System.CodeDom.Compiler.CodeDomProvider" />.
        /// </summary>
        [TaskAttribute("language", Required=true)]
        public string Language {
            get { return _language; }
            set { _language = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Any required references.
        /// </summary>
        [BuildElement("references")]
        public AssemblyFileSet References {
            get { return _references; }
            set { _references = value; }
        }

        /// <summary>
        /// The name of the main class containing the static <c>ScriptMain</c> 
        /// entry point. 
        /// </summary>
        [TaskAttribute("mainclass", Required=false)]
        public string MainClass {
            get { return _mainClass; }
            set { _mainClass = StringUtils.ConvertEmptyToNull(value); }
        }
        
        /// <summary>
        /// The namespace prefix for any custom functions defined in the script. 
        /// If ommitted the prefix will default to 'script'
        /// </summary>
        [TaskAttribute("prefix", Required=false)]
        public string Prefix {
            get { return _prefix; }
            set { _prefix = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// The namespaces to import.
        /// </summary>
        [BuildElement("imports")]
        public NamespaceImportCollection Imports {
            get { return _imports; }
            set { _imports = value; }
        }

        /// <summary>
        /// The code to execute.
        /// </summary>
        [BuildElement("code", Required=true)]
        public RawXml Code {
            get { return _code; }
            set { _code = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task

        /// <summary>
        /// Initializes the task using the specified xml node.
        /// </summary>
        protected override void InitializeTask(XmlNode taskNode) {
            _rootClassName = "nant" + Guid.NewGuid().ToString("N", 
                CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Executes the script block.
        /// </summary>
        protected override void ExecuteTask() {
            // create compiler info for user-specified language
            CompilerInfo compilerInfo = CreateCompilerInfo(Language);
               
            // ensure base directory is set, even if fileset was not initialized
            // from XML
            if (References.BaseDirectory == null) {
                References.BaseDirectory = new DirectoryInfo(Project.BaseDirectory);
            }

            ICodeCompiler compiler = compilerInfo.Compiler;
            CompilerParameters options = new CompilerParameters();
            options.GenerateExecutable = false;
            options.GenerateInMemory = true;
            options.MainClass = MainClass;

            // add all available assemblies.
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies()) {
                try {
                    if (!StringUtils.IsNullOrEmpty(asm.Location)) {
                        options.ReferencedAssemblies.Add(asm.Location);
                    }
                } catch (NotSupportedException) {
                    // Ignore - this error is sometimes thrown by asm.Location 
                    // for certain dynamic assemblies
                }
            }

            // add (and load) assemblies specified by user
            foreach (string assemblyFile in References.FileNames) {
                try {
                    // load assemblies into current AppDomain to ensure assemblies
                    // are available when executing the emitted assembly
                    Assembly.LoadFrom(assemblyFile);

                    // make assembly available to compiler
                    options.ReferencedAssemblies.Add(assemblyFile);
                } catch (Exception ex) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                        ResourceUtils.GetString("NA2028"), assemblyFile), Location, ex);
                }
            }
            
            StringCollection imports = new StringCollection();

            foreach (NamespaceImport import in Imports) {
                if (import.IfDefined && !import.UnlessDefined) {
                    imports.Add(import.Namespace);
                }
            }

            // generate the code
            CodeCompileUnit compileUnit = compilerInfo.GenerateCode(_rootClassName, 
                Code.Xml.InnerText, imports, Prefix);
            
            StringWriter sw = new StringWriter(CultureInfo.InvariantCulture);
            
            compilerInfo.CodeGen.GenerateCodeFromCompileUnit(compileUnit, sw, null);
            string code = sw.ToString();
            
            Log(Level.Debug, ResourceUtils.GetString("String_GeneratedCodeLooksLike") + "\n{0}", code);

            CompilerResults results = compiler.CompileAssemblyFromDom(options, compileUnit);

            Assembly compiled = null;
            if (results.Errors.Count > 0) {
                string errors = ResourceUtils.GetString("NA2029") + Environment.NewLine;
                foreach (CompilerError err in results.Errors) {
                    errors += err.ToString() + Environment.NewLine;
                }
                errors += code;
                throw new BuildException(errors, Location);
            } else {
                compiled = results.CompiledAssembly;
            }

            // scan the new assembly for tasks, types and functions
            // Its unlikely that tasks will be defined in buildfiles though.
            bool extensionAssembly = TypeFactory.ScanAssembly(compiled, this);
            
            string mainClass = _rootClassName;
            if (!StringUtils.IsNullOrEmpty(MainClass)) {
                mainClass += "+" + MainClass;
            }

            Type mainType = compiled.GetType(mainClass);
            if (mainType == null) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    ResourceUtils.GetString("NA2030"), mainClass), Location);
            }

            MethodInfo entry = mainType.GetMethod("ScriptMain");
            // check for task or function definitions.
            if (entry == null) {
                if (!extensionAssembly) {
                    throw new BuildException(ResourceUtils.GetString("NA2031"), Location);
                } else {
                    return; // no entry point so nothing to do here beyond loading task and function defs
                }
            }
            
            if (!entry.IsStatic) {
                throw new BuildException(ResourceUtils.GetString("NA2032"), Location);
            }

            ParameterInfo[] entryParams = entry.GetParameters();

            if (entryParams.Length != 1) {
                throw new BuildException(ResourceUtils.GetString("NA2033"), Location);
            }

            if (entryParams[0].ParameterType.FullName != typeof(Project).FullName) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    ResourceUtils.GetString("NA2034"), entryParams[0].ParameterType.FullName,
                    typeof(Project).FullName), Location);
            }
              
            try {
                // invoke Main method
                entry.Invoke(null, new object[] {Project});
            } catch (Exception ex) {
                // this exception is not likely to tell us much, BUT the 
                // InnerException normally contains the runtime exception
                // thrown by the executed script code.
                throw new BuildException(ResourceUtils.GetString("NA2035"), Location, 
                    ex.InnerException);
            }
        }

        #endregion Override implementation of Task

        #region Private Instance Methods

        private CompilerInfo CreateCompilerInfo(string language) {
            CodeDomProvider provider = null;
            LanguageId languageId;

            try {
                switch (language) {
                    case "vb":
                    case "VB":
                    case "VISUALBASIC":
                        languageId = LanguageId.VisualBasic;
                        provider = CreateCodeDomProvider(
                            "Microsoft.VisualBasic.VBCodeProvider",
                            "System, Culture=neutral");
                        break;
                    case "c#":
                    case "C#":
                    case "CSHARP":
                        languageId = LanguageId.CSharp;
                        provider = CreateCodeDomProvider(
                            "Microsoft.CSharp.CSharpCodeProvider",
                            "System, Culture=neutral");
                        break;
                    case "js":
                    case "JS":
                    case "JSCRIPT":
                        languageId = LanguageId.JScript;
                        provider = CreateCodeDomProvider(
                            "Microsoft.JScript.JScriptCodeProvider",
                            "Microsoft.JScript, Culture=neutral");
                        break;
                    case "vjs":
                    case "VJS":
                    case "JSHARP":
                        languageId = LanguageId.JSharp;
                        provider = CreateCodeDomProvider(
                            "Microsoft.VJSharp.VJSharpCodeProvider",
                            "VJSharpCodeProvider, Culture=neutral");
                        break;
                    default:
                        // if its not one of the above then it must be a fully 
                        // qualified provider class name
                        languageId = LanguageId.Other;
                        provider = CreateCodeDomProvider(language);
                        break;
                }

                return new CompilerInfo(languageId, provider);
            } catch (Exception ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    ResourceUtils.GetString("NA2036"), language), Location, ex);
            }
        }

        #endregion Private Instance Methods

        #region Private Static Methods

        private static CodeDomProvider CreateCodeDomProvider(string typeName, string assemblyName) {
            Assembly providerAssembly = Assembly.LoadWithPartialName(assemblyName);
            if (providerAssembly == null) {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                    ResourceUtils.GetString("NA2037"), assemblyName));
            }

            Type providerType = providerAssembly.GetType(typeName, true, true);
            return CreateCodeDomProvider(providerType);
        }

        private static CodeDomProvider CreateCodeDomProvider(string assemblyQualifiedTypeName) {
            Type providerType = Type.GetType(assemblyQualifiedTypeName, true, true);
            return CreateCodeDomProvider(providerType);
        }

        private static CodeDomProvider CreateCodeDomProvider(Type providerType) {
            object provider = Activator.CreateInstance(providerType);
            if (!(provider is CodeDomProvider)) {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                    ResourceUtils.GetString("NA2038"), providerType.FullName));
            }
            return (CodeDomProvider) provider;
        }

        #endregion Private Static Methods

        internal enum LanguageId : int {
            CSharp      = 1,
            VisualBasic = 2,
            JScript     = 3,
            JSharp      = 4,
            Other       = 5
        }

        internal class CompilerInfo {
            private LanguageId _lang;
            public readonly ICodeCompiler Compiler;
            public readonly ICodeGenerator CodeGen;

            public CompilerInfo(LanguageId languageId, CodeDomProvider provider) {
                _lang = languageId;

                Compiler = provider.CreateCompiler();
                CodeGen = provider.CreateGenerator();
            }


            public CodeCompileUnit GenerateCode(string typeName, string codeBody,
                                       StringCollection imports,
                                       string prefix) {
                CodeCompileUnit compileUnit = new CodeCompileUnit();
                
                CodeTypeDeclaration typeDecl = new CodeTypeDeclaration(typeName);
                typeDecl.IsClass = true;
                typeDecl.TypeAttributes = TypeAttributes.Public;
                
                // create constructor
                CodeConstructor constructMember = new CodeConstructor();
                constructMember.Attributes = MemberAttributes.Public;
                constructMember.Parameters.Add(new CodeParameterDeclarationExpression("Project", "project"));
                constructMember.Parameters.Add(new CodeParameterDeclarationExpression("PropertyDictionary", "propDict"));
                
                constructMember.BaseConstructorArgs.Add(new CodeVariableReferenceExpression("project"));
                constructMember.BaseConstructorArgs.Add(new CodeVariableReferenceExpression ("propDict"));
                typeDecl.Members.Add(constructMember);
                
                typeDecl.BaseTypes.Add(typeof(FunctionSetBase));
                
                // add FunctionSet attribute
                CodeAttributeDeclaration attrDecl = new CodeAttributeDeclaration("FunctionSet");
                attrDecl.Arguments.Add(new CodeAttributeArgument(
                    new CodeVariableReferenceExpression("\"" + prefix + "\"")));
                attrDecl.Arguments.Add(new CodeAttributeArgument(
                    new CodeVariableReferenceExpression("\"" + prefix + "\"")));
                
                typeDecl.CustomAttributes.Add(attrDecl);
                
                // pump in the user specified code as a snippet
                CodeSnippetTypeMember literalMember = 
                    new CodeSnippetTypeMember(codeBody);
                typeDecl.Members.Add( literalMember );
                
                CodeNamespace nspace = new CodeNamespace();

                //Add default imports
                foreach (string nameSpace in ScriptTask._defaultNamespaces) {
                    nspace.Imports.Add(new CodeNamespaceImport(nameSpace));
                }
                foreach (string nameSpace in imports) {
                    nspace.Imports.Add(new CodeNamespaceImport(nameSpace));
                }
                compileUnit.Namespaces.Add( nspace );
                nspace.Types.Add(typeDecl);
    
                
                return compileUnit;
            }
        }
    }
}
