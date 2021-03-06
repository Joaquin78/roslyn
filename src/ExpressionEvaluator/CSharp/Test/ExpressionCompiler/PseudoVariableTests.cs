// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class PseudoVariableTests : ExpressionCompilerTestBase
    {
        [Fact]
        public void UnrecognizedVariable()
        {
            var source =
@"class C
{
    static void M()
    {
    }
}";
            ResultProperties resultProperties;
            string error;
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.M",
                expr: "$v",
                resultProperties: out resultProperties,
                error: out error);
            Assert.Equal(error, "error CS0103: The name '$v' does not exist in the current context");
        }

        [Fact]
        public void GlobalName()
        {
            var source =
@"class C
{
    static void M()
    {
    }
}";
            ResultProperties resultProperties;
            string error;
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.M",
                expr: "global::$exception",
                resultProperties: out resultProperties,
                error: out error);
            Assert.Equal(error, "error CS0400: The type or namespace name '$exception' could not be found in the global namespace (are you missing an assembly reference?)");
        }

        [Fact]
        public void QualifiedName()
        {
            var source =
@"class C
{
    void M()
    {
    }
}";
            ResultProperties resultProperties;
            string error;
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.M",
                expr: "this.$exception",
                resultProperties: out resultProperties,
                error: out error);
            Assert.Equal(error, "error CS1061: 'C' does not contain a definition for '$exception' and no extension method '$exception' accepting a first argument of type 'C' could be found (are you missing a using directive or an assembly reference?)");
        }

        /// <summary>
        /// Generate call to intrinsic method for $exception,
        /// $stowedexception.
        /// </summary>
        [Fact]
        public void Exception()
        {
            var source =
@"class C
{
    static void M()
    {
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.M");
            ResultProperties resultProperties;
            string error;
            ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
            var testData = new CompilationTestData();
            var result = context.CompileExpression(
                InspectionContextFactory.Empty.Add("$exception", typeof(System.IO.IOException)).Add("$stowedexception", typeof(System.InvalidOperationException)),
                "(System.Exception)$exception ?? $stowedexception",
                DkmEvaluationFlags.TreatAsExpression,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
            Assert.Equal(testData.Methods.Count, 1);
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  call       ""System.Exception Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetException()""
  IL_0005:  castclass  ""System.IO.IOException""
  IL_000a:  dup
  IL_000b:  brtrue.s   IL_0018
  IL_000d:  pop
  IL_000e:  call       ""System.Exception Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetStowedException()""
  IL_0013:  castclass  ""System.InvalidOperationException""
  IL_0018:  ret
}");
        }

        [Fact]
        public void ReturnValue()
        {
            var source =
@"class C
{
    static void M()
    {
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.M");
            ResultProperties resultProperties;
            string error;
            ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
            var testData = new CompilationTestData();
            var result = context.CompileExpression(
                InspectionContextFactory.Empty.Add("$ReturnValue", typeof(object)).Add("$ReturnValue2", typeof(string)),
                "$ReturnValue ?? $ReturnValue2",
                DkmEvaluationFlags.TreatAsExpression,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
            Assert.Equal(testData.Methods.Count, 1);
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       22 (0x16)
  .maxstack  2
  IL_0000:  ldc.i4.0
  IL_0001:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetReturnValue(int)""
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_0015
  IL_0009:  pop
  IL_000a:  ldc.i4.2
  IL_000b:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetReturnValue(int)""
  IL_0010:  castclass  ""string""
  IL_0015:  ret
}");
            // Value type $ReturnValue.
            testData = new CompilationTestData();
            result = context.CompileExpression(
                InspectionContextFactory.Empty.Add("$ReturnValue", typeof(Nullable<int>)),
                "((int?)$ReturnValue).HasValue",
                DkmEvaluationFlags.TreatAsExpression,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (int? V_0)
  IL_0000:  ldc.i4.0
  IL_0001:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetReturnValue(int)""
  IL_0006:  unbox.any  ""int?""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  call       ""bool int?.HasValue.get""
  IL_0013:  ret
}");
        }

        /// <summary>
        /// Negative index should be treated as separate tokens.
        /// </summary>
        [Fact]
        public void ReturnValueNegative()
        {
            var source =
@"class C
{
    static void M()
    {
    }
}";
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.M",
                expr: "(int)$ReturnValue-2");
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldc.i4.0
  IL_0001:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetReturnValue(int)""
  IL_0006:  unbox.any  ""int""
  IL_000b:  ldc.i4.2
  IL_000c:  sub
  IL_000d:  ret
}");
        }

        /// <summary>
        /// Dev12 syntax "[0-9]+#" not supported.
        /// </summary>
        [WorkItem(1071347)]
        [Fact]
        public void ObjectId_EarlierSyntax()
        {
            var source =
@"class C
{
    static void M()
    {
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.M");
            ResultProperties resultProperties;
            string error;
            var testData = new CompilationTestData();
            context.CompileExpression(
                "23#",
                out resultProperties,
                out error,
                testData);
            Assert.Equal(error, "(1,1): error CS2043: 'id#' syntax is no longer supported. Use '$id' instead.");
        }

        [Fact]
        public void ObjectId()
        {
            var source =
@"class C
{
    static void M()
    {
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.M");
            ResultProperties resultProperties;
            string error;
            ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
            var testData = new CompilationTestData();
            context.CompileExpression(
                InspectionContextFactory.Empty.Add("23", typeof(string)).Add("4", typeof(Type)),
                "(object)$23 ?? $4.BaseType",
                DkmEvaluationFlags.TreatAsExpression,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
            Assert.Equal(testData.Methods.Count, 1);
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       40 (0x28)
  .maxstack  2
  IL_0000:  ldstr      ""23""
  IL_0005:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
  IL_000a:  castclass  ""string""
  IL_000f:  dup
  IL_0010:  brtrue.s   IL_0027
  IL_0012:  pop
  IL_0013:  ldstr      ""4""
  IL_0018:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
  IL_001d:  castclass  ""System.Type""
  IL_0022:  callvirt   ""System.Type System.Type.BaseType.get""
  IL_0027:  ret
}");
        }

        [WorkItem(1101017)]
        [Fact]
        public void NestedGenericValueType()
        {
            var source =
@"class C
{
    internal struct S<T>
    {
        internal T F;
    }
    static void M()
    {
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.M");
            ResultProperties resultProperties;
            string error;
            ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
            var testData = new CompilationTestData();
            context.CompileExpression(
                InspectionContextFactory.Empty.Add("s", "C+S`1[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]"),
                "s.F + 1",
                DkmEvaluationFlags.TreatAsExpression,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                null, // preferredUICulture 
                testData);
            Assert.Empty(missingAssemblyIdentities);
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldstr      ""s""
  IL_0005:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
  IL_000a:  unbox.any  ""C.S<int>""
  IL_000f:  ldfld      ""int C.S<int>.F""
  IL_0014:  ldc.i4.1
  IL_0015:  add
  IL_0016:  ret
}");
        }

        [Fact]
        public void ArrayType()
        {
            var source =
@"class C
{
    object F;
    static void M()
    {
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.M");
            ResultProperties resultProperties;
            string error;
            ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
            var testData = new CompilationTestData();
            context.CompileExpression(
                InspectionContextFactory.Empty.Add("a", "C[]").Add("b", "System.Int32[,], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"),
                "a[b[1, 0]].F",
                DkmEvaluationFlags.TreatAsExpression,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       44 (0x2c)
  .maxstack  4
  IL_0000:  ldstr      ""a""
  IL_0005:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
  IL_000a:  castclass  ""C[]""
  IL_000f:  ldstr      ""b""
  IL_0014:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
  IL_0019:  castclass  ""int[,]""
  IL_001e:  ldc.i4.1
  IL_001f:  ldc.i4.0
  IL_0020:  call       ""int[*,*].Get""
  IL_0025:  ldelem.ref
  IL_0026:  ldfld      ""object C.F""
  IL_002b:  ret
}");
        }

        /// <summary>
        /// The assembly-qualified type name may be from an
        /// unrecognized assembly. For instance, if the type was
        /// defined in a previous evaluation, say an anonymous
        /// type (e.g.: evaluate "o" after "var o = new { P = 1 };").
        /// </summary>
        [Fact]
        public void UnrecognizedAssembly()
        {
            var source =
@"struct S<T>
{
    internal T F;
}
class C
{
    static void M()
    {
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.M");
            ResultProperties resultProperties;
            string error;
            ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
            var testData = new CompilationTestData();

            // Unrecognized type.
            context.CompileExpression(
                InspectionContextFactory.Empty.Add("o", "T, 9BAC6622-86EB-4EC5-94A1-9A1E6D0C24AB, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                "o.P",
                DkmEvaluationFlags.TreatAsExpression,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
            Assert.Equal(error, "error CS0648: '' is a type not supported by the language");

            // Unrecognized array element type.
            context.CompileExpression(
                InspectionContextFactory.Empty.Add("a", "T[], 9BAC6622-86EB-4EC5-94A1-9A1E6D0C24AB, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                "a[0].P",
                DkmEvaluationFlags.TreatAsExpression,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
            Assert.Equal(error, "error CS0648: '' is a type not supported by the language");

            // Unrecognized generic type argument.
            context.CompileExpression(
                InspectionContextFactory.Empty.Add("s", "S`1[[T, 9BAC6622-86EB-4EC5-94A1-9A1E6D0C24AB, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]"),
                "s.F",
                DkmEvaluationFlags.TreatAsExpression,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
            Assert.Equal(error, "error CS0648: '' is a type not supported by the language");
        }

        [Fact]
        public void Variables()
        {
            CheckVariable("$exception", valid: true);
            CheckVariable("$stowedexception", valid: true);
            CheckVariable("$Exception", valid: false);
            CheckVariable("$STOWEDEXCEPTION", valid: false);
            CheckVariable("$ReturnValue", valid: true);
            CheckVariable("$RETURNVALUE", valid: false);
            CheckVariable("$returnvalue", valid: true); // Lowercase $ReturnValue supported.
            CheckVariable("$ReturnValue0", valid: true);
            CheckVariable("$returnvalue21", valid: true);
            CheckVariable("$ReturnValue3A", valid: false);
            CheckVariable("$33", valid: true);
            CheckVariable("$03", valid: false);
            CheckVariable("$3A", valid: false);
            CheckVariable("$0", valid: false);
            CheckVariable("$", valid: false);
            CheckVariable("$Unknown", valid: false);
        }

        private void CheckVariable(string variableName, bool valid)
        {
            var source =
@"class C
{
    static void M()
    {
    }
}";
            ResultProperties resultProperties;
            string error;
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.M",
                expr: variableName,
                resultProperties: out resultProperties,
                error: out error);
            if (valid)
            {
                var expectedNames = new[] { "<>x.<>m0()" };
                var actualNames = testData.Methods.Keys;
                AssertEx.SetEqual(expectedNames, actualNames);
            }
            else
            {
                Assert.Equal(error, string.Format("error CS0103: The name '{0}' does not exist in the current context", variableName));
            }
        }

        [Fact]
        public void CheckViability()
        {
            var source =
@"class C
{
    static void M()
    {
    }
}";
            ResultProperties resultProperties;
            string error;
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.M",
                expr: "$ReturnValue1<object>",
                resultProperties: out resultProperties,
                error: out error);
            Assert.Equal(error, "error CS0307: The variable '$ReturnValue1' cannot be used with type arguments");
            testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.M",
                expr: "$ReturnValue2()",
                resultProperties: out resultProperties,
                error: out error);
            Assert.Equal(error, "error CS0149: Method name expected");
        }

        /// <summary>
        /// $exception may be accessed from closure class.
        /// </summary>
        [Fact]
        public void ExceptionInDisplayClass()
        {
            var source =
@"using System;
class C
{
    static object F(System.Func<object> f)
    {
        return f();
    }
    static void M(object o)
    {
    }
}";
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.M",
                expr: "F(() => o ?? $exception)");
            testData.GetMethodData("<>x.<>c__DisplayClass0_0.<<>m0>b__0()").VerifyIL(
@"{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""object <>x.<>c__DisplayClass0_0.o""
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_000f
  IL_0009:  pop
  IL_000a:  call       ""System.Exception Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetException()""
  IL_000f:  ret
}");
        }

        [Fact]
        public void AssignException()
        {
            var source =
@"class C
{
    static void M(System.Exception e)
    {
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.M");
            string error;
            var testData = new CompilationTestData();
            context.CompileAssignment(
                target: "e",
                expr: "$exception.InnerException ?? $exception",
                error: out error,
                testData: testData);
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       22 (0x16)
  .maxstack  2
  IL_0000:  call       ""System.Exception Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetException()""
  IL_0005:  callvirt   ""System.Exception System.Exception.InnerException.get""
  IL_000a:  dup
  IL_000b:  brtrue.s   IL_0013
  IL_000d:  pop
  IL_000e:  call       ""System.Exception Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetException()""
  IL_0013:  starg.s    V_0
  IL_0015:  ret
}");
        }

        [Fact]
        public void AssignToException()
        {
            var source =
@"class C
{
    static void M()
    {
    }
}";
            ResultProperties resultProperties;
            string error;
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.M",
                expr: "$exception = null",
                resultProperties: out resultProperties,
                error: out error);
            Assert.Equal(error, "error CS0131: The left-hand side of an assignment must be a variable, property or indexer");
        }

        [WorkItem(1100849)]
        [Fact]
        public void PassByRef()
        {
            var source =
@"class C
{
    static T F<T>(ref T t)
    {
        t = default(T);
        return t;
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.F");
            ResultProperties resultProperties;
            string error;
            ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;

            // $exception
            var testData = new CompilationTestData();
            context.CompileExpression(
                DefaultInspectionContext.Instance,
                "$exception = null",
                DkmEvaluationFlags.TreatAsExpression,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
            Assert.Equal(error, "error CS0131: The left-hand side of an assignment must be a variable, property or indexer");
            testData = new CompilationTestData();
            context.CompileExpression(
                DefaultInspectionContext.Instance,
                "F(ref $exception)",
                DkmEvaluationFlags.TreatAsExpression,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
            Assert.Equal(error, "error CS1510: A ref or out argument must be an assignable variable");

            // Object at address
            testData = new CompilationTestData();
            context.CompileExpression(
                DefaultInspectionContext.Instance,
                "@0x123 = null",
                DkmEvaluationFlags.TreatAsExpression,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Equal(error, "error CS0131: The left-hand side of an assignment must be a variable, property or indexer");
            testData = new CompilationTestData();
            context.CompileExpression(
                DefaultInspectionContext.Instance,
                "F(ref @0x123)",
                DkmEvaluationFlags.TreatAsExpression,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
            Assert.Equal(error, "error CS1510: A ref or out argument must be an assignable variable");

            // $ReturnValue
            testData = new CompilationTestData();
            context.CompileExpression(
                DefaultInspectionContext.Instance,
                "$ReturnValue = null",
                DkmEvaluationFlags.TreatAsExpression,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
            Assert.Equal(error, "error CS0131: The left-hand side of an assignment must be a variable, property or indexer");
            testData = new CompilationTestData();
            context.CompileExpression(
                DefaultInspectionContext.Instance,
                "F(ref $ReturnValue)",
                DkmEvaluationFlags.TreatAsExpression,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
            Assert.Equal(error, "error CS1510: A ref or out argument must be an assignable variable");

            // Object id
            testData = new CompilationTestData();
            context.CompileExpression(
                DefaultInspectionContext.Instance,
                "$1 = null",
                DkmEvaluationFlags.TreatAsExpression,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
            Assert.Equal(error, "error CS0131: The left-hand side of an assignment must be a variable, property or indexer");
            testData = new CompilationTestData();
            context.CompileExpression(
                DefaultInspectionContext.Instance,
                "F(ref $1)",
                DkmEvaluationFlags.TreatAsExpression,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
            Assert.Equal(error, "error CS1510: A ref or out argument must be an assignable variable");

            // Declared variable
            testData = new CompilationTestData();
            context.CompileExpression(
                InspectionContextFactory.Empty.Add("x", typeof(int)),
                "x = 1",
                DkmEvaluationFlags.TreatAsExpression,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
            Assert.Null(error);
            testData.GetMethodData("<>x.<>m0<T>").VerifyIL(
@"{
  // Code size       16 (0x10)
  .maxstack  3
  .locals init (T V_0,
                int V_1)
  IL_0000:  ldstr      ""x""
  IL_0005:  call       ""int Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<int>(string)""
  IL_000a:  ldc.i4.1
  IL_000b:  dup
  IL_000c:  stloc.1
  IL_000d:  stind.i4
  IL_000e:  ldloc.1
  IL_000f:  ret
}");
            testData = new CompilationTestData();
            var result = context.CompileExpression(
                InspectionContextFactory.Empty.Add("x", typeof(int)),
                "F(ref x)",
                DkmEvaluationFlags.TreatAsExpression,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
            Assert.Null(error);
            testData.GetMethodData("<>x.<>m0<T>").VerifyIL(
@"{
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (T V_0)
  IL_0000:  ldstr      ""x""
  IL_0005:  call       ""int Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<int>(string)""
  IL_000a:  call       ""int C.F<int>(ref int)""
  IL_000f:  ret
}");
        }

        [Fact]
        public void ValueType()
        {
            var source =
@"struct S
{
    internal object F;
}
class C
{
    static void M()
    {
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.M");
            ResultProperties resultProperties;
            string error;
            ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
            var testData = new CompilationTestData();
            context.CompileExpression(
                InspectionContextFactory.Empty.Add("s", "S"),
                "s.F = 1",
                DkmEvaluationFlags.TreatAsExpression,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       25 (0x19)
  .maxstack  3
  .locals init (object V_0)
  IL_0000:  ldstr      ""s""
  IL_0005:  call       ""S Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<S>(string)""
  IL_000a:  ldc.i4.1
  IL_000b:  box        ""int""
  IL_0010:  dup
  IL_0011:  stloc.0
  IL_0012:  stfld      ""object S.F""
  IL_0017:  ldloc.0
  IL_0018:  ret
}");
        }

        [Fact]
        public void CompoundAssignment()
        {
            var source =
@"struct S
{
    internal int F;
}
class C
{
    static void M()
    {
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.M");
            ResultProperties resultProperties;
            string error;
            ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
            var testData = new CompilationTestData();
            context.CompileExpression(
                InspectionContextFactory.Empty.Add("s", "S"),
                "s.F += 2",
                DkmEvaluationFlags.TreatAsExpression,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       24 (0x18)
  .maxstack  3
  .locals init (int V_0)
  IL_0000:  ldstr      ""s""
  IL_0005:  call       ""S Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<S>(string)""
  IL_000a:  ldflda     ""int S.F""
  IL_000f:  dup
  IL_0010:  ldind.i4
  IL_0011:  ldc.i4.2
  IL_0012:  add
  IL_0013:  dup
  IL_0014:  stloc.0
  IL_0015:  stind.i4
  IL_0016:  ldloc.0
  IL_0017:  ret
}");
        }

        /// <summary>
        /// Assembly-qualified type names from the debugger refer to runtime assemblies
        /// which may be different versions than the assembly references in metadata.
        /// </summary>
        [WorkItem(1087458)]
        [Fact]
        public void DifferentAssemblyVersion()
        {
            var sourceA =
@"public class A<T>
{
}";
            var sourceB =
@"class B<T>
{
}
class C
{
    static void M()
    {
        var o = new A<object>();
    }
}";
            var assemblyNameA = "397300B0-A";
            var publicKeyA = ImmutableArray.CreateRange(new byte[] { 0x00, 0x24, 0x00, 0x00, 0x04, 0x80, 0x00, 0x00, 0x94, 0x00, 0x00, 0x00, 0x06, 0x02, 0x00, 0x00, 0x00, 0x24, 0x00, 0x00, 0x52, 0x53, 0x41, 0x31, 0x00, 0x04, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0xED, 0xD3, 0x22, 0xCB, 0x6B, 0xF8, 0xD4, 0xA2, 0xFC, 0xCC, 0x87, 0x37, 0x04, 0x06, 0x04, 0xCE, 0xE7, 0xB2, 0xA6, 0xF8, 0x4A, 0xEE, 0xF3, 0x19, 0xDF, 0x5B, 0x95, 0xE3, 0x7A, 0x6A, 0x28, 0x24, 0xA4, 0x0A, 0x83, 0x83, 0xBD, 0xBA, 0xF2, 0xF2, 0x52, 0x20, 0xE9, 0xAA, 0x3B, 0xD1, 0xDD, 0xE4, 0x9A, 0x9A, 0x9C, 0xC0, 0x30, 0x8F, 0x01, 0x40, 0x06, 0xE0, 0x2B, 0x95, 0x62, 0x89, 0x2A, 0x34, 0x75, 0x22, 0x68, 0x64, 0x6E, 0x7C, 0x2E, 0x83, 0x50, 0x5A, 0xCE, 0x7B, 0x0B, 0xE8, 0xF8, 0x71, 0xE6, 0xF7, 0x73, 0x8E, 0xEB, 0x84, 0xD2, 0x73, 0x5D, 0x9D, 0xBE, 0x5E, 0xF5, 0x90, 0xF9, 0xAB, 0x0A, 0x10, 0x7E, 0x23, 0x48, 0xF4, 0xAD, 0x70, 0x2E, 0xF7, 0xD4, 0x51, 0xD5, 0x8B, 0x3A, 0xF7, 0xCA, 0x90, 0x4C, 0xDC, 0x80, 0x19, 0x26, 0x65, 0xC9, 0x37, 0xBD, 0x52, 0x81, 0xF1, 0x8B, 0xCD });
            var compilationA1 = CreateCompilation(
                new AssemblyIdentity(assemblyNameA, new Version(1, 1, 1, 1), cultureName: "", publicKeyOrToken: publicKeyA, hasPublicKey: true),
                new[] { sourceA },
                references: new[] { MscorlibRef_v20 },
                options: TestOptions.DebugDll.WithDelaySign(true));
            var referenceA1 = compilationA1.EmitToImageReference();
            var assemblyNameB = "397300B0-B";
            var compilationB1 = CreateCompilation(
                new AssemblyIdentity(assemblyNameB, new Version(1, 2, 2, 2)),
                new[] { sourceB },
                references: new[] { MscorlibRef_v20, referenceA1 },
                options: TestOptions.DebugDll);

            // Use mscorlib v4.0.0.0 and A v2.1.2.1 at runtime.
            byte[] exeBytes;
            byte[] pdbBytes;
            ImmutableArray<MetadataReference> references;
            compilationB1.EmitAndGetReferences(out exeBytes, out pdbBytes, out references);
            var compilationA2 = CreateCompilation(
                new AssemblyIdentity(assemblyNameA, new Version(2, 1, 2, 1), cultureName: "", publicKeyOrToken: publicKeyA, hasPublicKey: true),
                new[] { sourceA },
                references: new[] { MscorlibRef_v20 },
                options: TestOptions.DebugDll.WithDelaySign(true));
            var referenceA2 = compilationA2.EmitToImageReference();
            var runtime = CreateRuntimeInstance(
                assemblyNameB,
                ImmutableArray.Create(MscorlibRef, referenceA2).AddIntrinsicAssembly(),
                exeBytes,
                new SymReader(pdbBytes));

            var context = CreateMethodContext(runtime, "C.M");
            ResultProperties resultProperties;
            string error;
            ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
            var testData = new CompilationTestData();
            context.CompileExpression(
                // typeof(Exception), typeof(A<B<object>>), typeof(B<A<object>[]>)
                InspectionContextFactory.Empty.Add("$exception", "System.Exception, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089").
                    Add("1", "A`1[[B`1[[System.Object, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], 397300B0-B, Version=1.2.2.2, Culture=neutral, PublicKeyToken=null]], 397300B0-A, Version=2.1.2.1, Culture=neutral, PublicKeyToken=null").
                    Add("2", "B`1[[A`1[[System.Object, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]][], 397300B0-A, Version=2.1.2.1, Culture=neutral, PublicKeyToken=null]], 397300B0-B, Version=1.2.2.2, Culture=neutral, PublicKeyToken=null"),
                "(object)$exception ?? (object)$1 ?? $2",
                DkmEvaluationFlags.TreatAsExpression,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       44 (0x2c)
  .maxstack  2
  .locals init (A<object> V_0) //o
  IL_0000:  call       ""System.Exception Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetException()""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_002b
  IL_0008:  pop
  IL_0009:  ldstr      ""1""
  IL_000e:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
  IL_0013:  castclass  ""A<B<object>>""
  IL_0018:  dup
  IL_0019:  brtrue.s   IL_002b
  IL_001b:  pop
  IL_001c:  ldstr      ""2""
  IL_0021:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
  IL_0026:  castclass  ""B<A<object>[]>""
  IL_002b:  ret
}");
        }

        /// <summary>
        /// The assembly-qualified type may reference an assembly
        /// outside of the current module and its references.
        /// </summary>
        [WorkItem(1092680)]
        [Fact]
        public void TypeOutsideModule()
        {
            var sourceA =
@"using System;
public class A<T>
{
    public static void M(Action f)
    {
        object o;
        try
        {
            f();
        }
        catch (Exception)
        {
        }
    }
}";
            var sourceB =
@"using System;
class E : Exception
{
    internal object F;
}
class B
{
    static void Main()
    {
        A<int>.M(() => { throw new E(); });
    }
}";
            var assemblyNameA = "0A93FF0B-31A2-47C8-B24D-16A2D77AB5C5";
            var compilationA = CreateCompilationWithMscorlib(sourceA, options: TestOptions.DebugDll, assemblyName: assemblyNameA);
            byte[] exeA;
            byte[] pdbA;
            ImmutableArray<MetadataReference> referencesA;
            compilationA.EmitAndGetReferences(out exeA, out pdbA, out referencesA);
            var metadataA = AssemblyMetadata.CreateFromImage(exeA);
            var referenceA = metadataA.GetReference();

            var assemblyNameB = "9BAC6622-86EB-4EC5-94A1-9A1E6D0C24B9";
            var compilationB = CreateCompilationWithMscorlib(sourceB, options: TestOptions.DebugExe, references: new[] { referenceA }, assemblyName: assemblyNameB);
            byte[] exeB;
            byte[] pdbB;
            ImmutableArray<MetadataReference> referencesB;
            compilationB.EmitAndGetReferences(out exeB, out pdbB, out referencesB);
            var metadataB = AssemblyMetadata.CreateFromImage(exeB);
            var referenceB = metadataB.GetReference();

            var modulesBuilder = ArrayBuilder<ModuleInstance>.GetInstance();
            modulesBuilder.Add(MscorlibRef.ToModuleInstance(fullImage: null, symReader: null));
            modulesBuilder.Add(referenceA.ToModuleInstance(fullImage: exeA, symReader: new SymReader(pdbA)));
            modulesBuilder.Add(referenceB.ToModuleInstance(fullImage: exeB, symReader: new SymReader(pdbB)));
            modulesBuilder.Add(ExpressionCompilerTestHelpers.IntrinsicAssemblyReference.ToModuleInstance(fullImage: null, symReader: null));

            using (var runtime = new RuntimeInstance(modulesBuilder.ToImmutableAndFree()))
            {
                var context = CreateMethodContext(runtime, "A.M");
                ResultProperties resultProperties;
                string error;
                ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
                var testData = new CompilationTestData();
                context.CompileExpression(
                    InspectionContextFactory.Empty.Add("$exception", "E, 9BAC6622-86EB-4EC5-94A1-9A1E6D0C24B9, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                    "$exception",
                    DkmEvaluationFlags.TreatAsExpression,
                    DiagnosticFormatter.Instance,
                    out resultProperties,
                    out error,
                    out missingAssemblyIdentities,
                    EnsureEnglishUICulture.PreferredOrNull,
                    testData);
                Assert.Empty(missingAssemblyIdentities);
                testData.GetMethodData("<>x<T>.<>m0").VerifyIL(
@"{
  // Code size       11 (0xb)
  .maxstack  1
  .locals init (object V_0) //o
  IL_0000:  call       ""System.Exception Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetException()""
  IL_0005:  castclass  ""E""
  IL_000a:  ret
}");
                testData = new CompilationTestData();
                context.CompileAssignment(
                    InspectionContextFactory.Empty.Add("1", "A`1[[B, 9BAC6622-86EB-4EC5-94A1-9A1E6D0C24B9, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]], 0A93FF0B-31A2-47C8-B24D-16A2D77AB5C5, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                    "o",
                    "$1",
                    DiagnosticFormatter.Instance,
                    out resultProperties,
                    out error,
                    out missingAssemblyIdentities,
                    EnsureEnglishUICulture.PreferredOrNull,
                    testData);
                Assert.Empty(missingAssemblyIdentities);
                testData.GetMethodData("<>x<T>.<>m0").VerifyIL(
@"{
  // Code size       17 (0x11)
  .maxstack  1
  .locals init (object V_0) //o
  IL_0000:  ldstr      ""1""
  IL_0005:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
  IL_000a:  castclass  ""A<B>""
  IL_000f:  stloc.0
  IL_0010:  ret
}");
            }
        }
    }
}
