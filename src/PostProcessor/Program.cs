using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

class Program
{
    static void Main(string[] args)
    {
        var assembly = args[0];

        var readerParameters = new ReaderParameters
        {
            ReadSymbols = true
        };

        var assemblyDefinition = AssemblyDefinition.ReadAssembly(assembly, readerParameters);

        var typeFinder = new TypeFinder(assemblyDefinition.MainModule.AssemblyResolver, assemblyDefinition.MainModule);

        FixEvents(typeFinder, assemblyDefinition.MainModule);

        var writeParameters = new WriterParameters
        {
            WriteSymbols = true
        };

        assemblyDefinition.Write(assembly);
    }

    private static void FixEvents(TypeFinder typeFinder, ModuleDefinition module)
    {
        var interlocked = typeFinder.GetMSCorLibTypeDefinition("System.Threading.Interlocked");
        var compareExchange = module.Import(interlocked.Methods.First(m => m.Name == "CompareExchange" && !m.HasGenericParameters && m.ReturnType.FullName == "System.Object"));

        foreach (var type in module.GetTypes())
        {
            foreach (var evnt in type.Events)
            {
                FixEventMethod(compareExchange, evnt.AddMethod);
                FixEventMethod(compareExchange, evnt.RemoveMethod);
            }
        }
    }

    private static void FixEventMethod(MethodReference compareExchange, MethodDefinition method)
    {
        if (!method.IsCompilerGenerated())
            return;

        var instructions = method.Body.Instructions;

        for (int i = 0; i < instructions.Count; i++)
        {
            var m = instructions[i].Operand as MethodReference;
            if (m?.Name != "CompareExchange")
                continue;

            instructions[i] = Instruction.Create(OpCodes.Call, compareExchange);
            break;
        }
    }
}