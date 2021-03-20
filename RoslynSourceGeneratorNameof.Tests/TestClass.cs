using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace RoslynSourceGeneratorNameof.Tests
{
    public class TestClass
    {

        private const string Sample = @"
using System.Diagnostics.CodeAnalysis;
public partial class MyClass
{
    [MemberNotNull(nameof(GeneratedProp))]
    void Foo()
    {}

    public static void Main() {}
}";

        [Fact]
        public void CodeCompiles()
        {
            Compilation input = CreateCompilation(Sample);
            var generator = new MyBuggyGenerator();

            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

            driver = driver.RunGeneratorsAndUpdateCompilation(input, out var outputCompilation, out var diagnostics);

            Debug.Assert(diagnostics.IsEmpty); // fail here: warning CS8785: Generator 'MyClass' failed to generate source. It will not contribute to the output and compilation errors may occur as a result. Exception was of type 'Exception' with message 'here's bug!'
            Debug.Assert(outputCompilation.GetDiagnostics().IsEmpty);
            Debug.Assert(outputCompilation.SyntaxTrees.Count() == 2);
        }

        private static Compilation CreateCompilation(string source)
            => CSharpCompilation
                .Create(
                    "compilation",
                    new[] { CSharpSyntaxTree.ParseText(source) },
                    new[]
                    {
                        GetReference<Binder>(),
                        GetReference<MemberNotNullAttribute>(),
                    },
                    new CSharpCompilationOptions(OutputKind.ConsoleApplication));

        private static PortableExecutableReference GetReference<T>()
        {
            return MetadataReference.CreateFromFile(typeof(T).GetTypeInfo().Assembly.Location);
        }
    }


}
