using BenchmarkDotNet.Attributes;

namespace SonolusPerformanceTests;

public class Benchmarks
{
    private readonly Context context = new();
    
    private readonly INode fib = Nodes.Execute(
        Nodes.Set(1.Node(), 0.Node()),
        Nodes.Set(2.Node(), 1.Node()),
        Nodes.While(Nodes.Get(0.Node()),
            Nodes.Execute(
                Nodes.Set(3.Node(), Nodes.Add(Nodes.Get(1.Node()), Nodes.Get(2.Node()))),
                Nodes.Set(1.Node(), Nodes.Get(2.Node())),
                Nodes.Set(2.Node(), Nodes.Get(3.Node())),
                Nodes.Set(0.Node(), Nodes.Sub(Nodes.Get(0.Node()), 1.Node())))),
        Nodes.Get(1.Node()));
    private readonly NodeClosure fibClosure;
    private readonly Instruction[] fibInstructions;
    
    // Takes the number of inputs at index 0, and the inputs at indices 1 to n
    // Performs an insertion sort on the inputs, then sums ever other input starting from the first and returns the sum
    private readonly INode insertionSortAndSumEveryOther = Nodes.Execute(
        Nodes.Set(1001.Node(), 1.Node()),
        Nodes.While(Nodes.Lte(Nodes.Get(1001.Node()), Nodes.Get(0.Node())),
            Nodes.Execute(
                Nodes.Set(1002.Node(), Nodes.Get(1001.Node())),
                Nodes.Set(1003.Node(), Nodes.Add(Nodes.Get(1001.Node()), 1.Node())),
                Nodes.While(Nodes.Lt(Nodes.Get(1003.Node()), Nodes.Get(0.Node())),
                    Nodes.Execute(
                        Nodes.If(Nodes.Lt(Nodes.Get(Nodes.Get(1003.Node())), Nodes.Get(Nodes.Get(1002.Node()))),
                            Nodes.Execute(
                                Nodes.Set(1004.Node(), Nodes.Get(Nodes.Get(1003.Node()))),
                                Nodes.Set(Nodes.Get(1003.Node()), Nodes.Get(Nodes.Get(1002.Node()))),
                                Nodes.Set(Nodes.Get(1002.Node()), Nodes.Get(1004.Node()))), 0.Node()),
                        Nodes.Set(1003.Node(), Nodes.Add(Nodes.Get(1003.Node()), 1.Node())))),
                Nodes.Set(1001.Node(), Nodes.Add(Nodes.Get(1001.Node()), 1.Node())))),
        Nodes.Set(1001.Node(), 1.Node()),
        Nodes.Set(1002.Node(), 0.Node()),
        Nodes.While(Nodes.Lte(Nodes.Get(1001.Node()), Nodes.Get(0.Node())),
            Nodes.Execute(
                Nodes.If(Nodes.Eq(Nodes.Mod(Nodes.Get(1001.Node()), 2.Node()), 0.Node()),
                    Nodes.Set(1002.Node(), Nodes.Add(Nodes.Get(1002.Node()), Nodes.Get(Nodes.Get(1001.Node())))), 0.Node()),
                Nodes.Set(1001.Node(), Nodes.Add(Nodes.Get(1001.Node()), 1.Node())))),
        Nodes.Get(1002.Node()));
    private readonly NodeClosure insertionSortAndSumEveryOtherClosure;
    private readonly Instruction[] insertionSortAndSumEveryOtherInstructions;
    
    public Benchmarks()
    {
        fibClosure = fib.Closure();
        fibInstructions = Instruction.Compile(fib);
        insertionSortAndSumEveryOtherClosure = insertionSortAndSumEveryOther.Closure();
        insertionSortAndSumEveryOtherInstructions = Instruction.Compile(insertionSortAndSumEveryOther);
    }
    
    [Benchmark]
    public double FibEvaluate()
    {
        context.Memory[0] = 1000;
        return fib.Evaluate(context);
    }
    
    [Benchmark]
    public double FibClosure()
    {
        context.Memory[0] = 1000;
        return fibClosure(context);
    }
    
    [Benchmark]
    public double FibBytecode()
    {
        context.Memory[0] = 1000;
        return BytecodeInterpreter.Run(fibInstructions, context);
    }
    
    [Benchmark]
    public double InsertionSortAndSumEveryOtherEvaluate()
    {
        context.Memory[0] = 100;
        for (var i = 1; i <= 100; i++)
        {
            context.Memory[i] = 100 - i;
        }
        return insertionSortAndSumEveryOther.Evaluate(context);
    }
    
    [Benchmark]
    public double InsertionSortAndSumEveryOtherClosure()
    {
        context.Memory[0] = 100;
        for (var i = 1; i <= 100; i++)
        {
            context.Memory[i] = 100 - i;
        }
        return insertionSortAndSumEveryOtherClosure(context);
    }
    
    [Benchmark]
    public double InsertionSortAndSumEveryOtherBytecode()
    {
        context.Memory[0] = 100;
        for (var i = 1; i <= 100; i++)
        {
            context.Memory[i] = 100 - i;
        }
        return BytecodeInterpreter.Run(insertionSortAndSumEveryOtherInstructions, context);
    }
}
