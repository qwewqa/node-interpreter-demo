using BenchmarkDotNet.Running;
using SonolusPerformanceTests;

// |                                Method |      Mean |    Error |    StdDev |    Median |
// |-------------------------------------- |----------:|---------:|----------:|----------:|
// |                           FibEvaluate |  50.85 us | 0.552 us |  0.517 us |  50.94 us |
// |                            FibClosure |  42.72 us | 0.815 us |  1.001 us |  42.89 us |
// |                           FibBytecode |  33.13 us | 0.631 us |  0.620 us |  33.32 us |
// | InsertionSortAndSumEveryOtherEvaluate | 474.40 us | 9.169 us | 13.439 us | 473.92 us |
// |  InsertionSortAndSumEveryOtherClosure | 372.72 us | 6.348 us |  6.234 us | 370.88 us |
// | InsertionSortAndSumEveryOtherBytecode | 272.11 us | 5.342 us |  9.769 us | 267.56 us |


// var bm = new Benchmarks();
// Console.WriteLine(bm.FibEvaluate());
// Console.WriteLine(bm.FibClosure());
// Console.WriteLine(bm.FibBytecode());
// Console.WriteLine(bm.InsertionSortAndSumEveryOtherEvaluate());
// Console.WriteLine(bm.InsertionSortAndSumEveryOtherClosure());
// Console.WriteLine(bm.InsertionSortAndSumEveryOtherBytecode());

BenchmarkRunner.Run<Benchmarks>();
