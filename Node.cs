namespace SonolusPerformanceTests;

public delegate double NodeClosure(Context ctx);

public interface INode
{
    public double Evaluate(Context ctx);
    public NodeClosure Closure();
    public void Compile(List<Instruction> instructions, bool useValue);
}

public class ValueNode : INode
{
    public double Value { get; init; }

    public double Evaluate(Context ctx)
    {
        return Value;
    }

    public NodeClosure Closure()
    {
        return _ => Value;
    }

    public void Compile(List<Instruction> instructions, bool useValue)
    {
        if (useValue)
        {
            instructions.Add(new Instruction(OpCode.PUSH, Value));
        }
    }
}

public class ExecuteNode : INode
{
    public INode[] Nodes { get; init; }

    public double Evaluate(Context ctx)
    {
        double result = 0;
        foreach (var node in Nodes)
        {
            result = node.Evaluate(ctx);
        }

        return result;
    }

    public NodeClosure Closure()
    {
        var closures = Nodes.Select(n => n.Closure()).ToArray();
        return ctx =>
        {
            double result = 0;
            foreach (var closure in closures)
            {
                result = closure(ctx);
            }

            return result;
        };
    }

    public void Compile(List<Instruction> instructions, bool useValue)
    {
        for (var i = 0; i < Nodes.Length - 1; i++)
        {
            Nodes[i].Compile(instructions, false);
        }

        if (Nodes.Length > 0)
        {
            Nodes[^1].Compile(instructions, useValue);
        }
        else if (useValue)
        {
            instructions.Add(new Instruction(OpCode.PUSH, 0));
        }
    }
}

public class IfNode : INode
{
    public INode Condition { get; init; }
    public INode True { get; init; }
    public INode False { get; init; }

    public double Evaluate(Context ctx)
    {
        return Condition.Evaluate(ctx) != 0 ? True.Evaluate(ctx) : False.Evaluate(ctx);
    }

    public NodeClosure Closure()
    {
        var condition = Condition.Closure();
        var trueClosure = True.Closure();
        var falseClosure = False.Closure();
        return ctx => condition(ctx) != 0 ? trueClosure(ctx) : falseClosure(ctx);
    }

    public void Compile(List<Instruction> instructions, bool useValue)
    {
        Condition.Compile(instructions, true);
        instructions.Add(Instruction.NOOP);
        var jmpIfFalse = instructions.Count - 1;
        True.Compile(instructions, useValue);
        instructions.Add(Instruction.NOOP);
        var jmp = instructions.Count - 1;
        instructions[jmpIfFalse] = new Instruction(OpCode.POP_JMP_IF_FALSE, instructions.Count);
        False.Compile(instructions, useValue);
        instructions[jmp] = new Instruction(OpCode.JMP, instructions.Count);
    }
}

public class WhileNode : INode
{
    public INode Condition { get; init; }
    public INode Body { get; init; }

    public double Evaluate(Context ctx)
    {
        while (Condition.Evaluate(ctx) != 0)
        {
            Body.Evaluate(ctx);
        }

        return 0.0;
    }

    public NodeClosure Closure()
    {
        var condition = Condition.Closure();
        var body = Body.Closure();
        return ctx =>
        {
            while (condition(ctx) != 0)
            {
                body(ctx);
            }

            return 0;
        };
    }

    public void Compile(List<Instruction> instructions, bool useValue)
    {
        var jmpTarget = instructions.Count; // Since condition always pushes, we know this is valid
        Condition.Compile(instructions, true);
        instructions.Add(Instruction.NOOP);
        var jmpIfFalse = instructions.Count - 1;
        Body.Compile(instructions, false);
        instructions.Add(new Instruction(OpCode.JMP, jmpTarget));
        instructions[jmpIfFalse] = new Instruction(OpCode.POP_JMP_IF_FALSE, instructions.Count);
    }
}

public class GetNode : INode
{
    public INode Index { get; init; }

    public double Evaluate(Context ctx)
    {
        return ctx.Memory[(int)Index.Evaluate(ctx)];
    }

    public NodeClosure Closure()
    {
        var index = Index.Closure();
        return ctx => ctx.Memory[(int)index(ctx)];
    }

    public void Compile(List<Instruction> instructions, bool useValue)
    {
        if (!useValue)
        {
            return;
        }

        // check if index is constant
        if (Index is ValueNode valueNode)
        {
            instructions.Add(new Instruction(OpCode.GET, (int)valueNode.Value));
        }
        else
        {
            Index.Compile(instructions, true);
            instructions.Add(new Instruction(OpCode.GET_INDIRECT, 0));
        }
    }
}

public class SetNode : INode
{
    public INode Index { get; init; }
    public INode Value { get; init; }

    public double Evaluate(Context ctx)
    {
        return ctx.Memory[(int)Index.Evaluate(ctx)] = Value.Evaluate(ctx);
    }

    public NodeClosure Closure()
    {
        var index = Index.Closure();
        var value = Value.Closure();
        return ctx => ctx.Memory[(int)index(ctx)] = value(ctx);
    }

    public void Compile(List<Instruction> instructions, bool useValue)
    {
        // check if index is constant
        if (Index is ValueNode valueNode)
        {
            Value.Compile(instructions, true);
            instructions.Add(new Instruction(OpCode.SET, (int)valueNode.Value));
        }
        else
        {
            Index.Compile(instructions, true);
            Value.Compile(instructions, true);
            instructions.Add(new Instruction(OpCode.SET_INDIRECT, 0));
        }
    }
}

public class AddNode : INode
{
    public INode Left { get; init; }
    public INode Right { get; init; }

    public double Evaluate(Context ctx)
    {
        return Left.Evaluate(ctx) + Right.Evaluate(ctx);
    }

    public NodeClosure Closure()
    {
        var left = Left.Closure();
        var right = Right.Closure();
        return ctx => left(ctx) + right(ctx);
    }

    public void Compile(List<Instruction> instructions, bool useValue)
    {
        if (useValue)
        {
            Left.Compile(instructions, true);
            Right.Compile(instructions, true);
            instructions.Add(new Instruction(OpCode.ADD, 0));
        }
        else
        {
            Left.Compile(instructions, false);
            Right.Compile(instructions, false);
        }
    }
}

public class SubNode : INode
{
    public INode Left { get; init; }
    public INode Right { get; init; }

    public double Evaluate(Context ctx)
    {
        return Left.Evaluate(ctx) - Right.Evaluate(ctx);
    }

    public NodeClosure Closure()
    {
        var left = Left.Closure();
        var right = Right.Closure();
        return ctx => left(ctx) - right(ctx);
    }

    public void Compile(List<Instruction> instructions, bool useValue)
    {
        if (useValue)
        {
            Left.Compile(instructions, true);
            Right.Compile(instructions, true);
            instructions.Add(new Instruction(OpCode.SUB, 0));
        }
        else
        {
            Left.Compile(instructions, false);
            Right.Compile(instructions, false);
        }
    }
}

public class MulNode : INode
{
    public INode Left { get; init; }
    public INode Right { get; init; }

    public double Evaluate(Context ctx)
    {
        return Left.Evaluate(ctx) * Right.Evaluate(ctx);
    }

    public NodeClosure Closure()
    {
        var left = Left.Closure();
        var right = Right.Closure();
        return ctx => left(ctx) * right(ctx);
    }

    public void Compile(List<Instruction> instructions, bool useValue)
    {
        if (useValue)
        {
            Left.Compile(instructions, true);
            Right.Compile(instructions, true);
            instructions.Add(new Instruction(OpCode.MUL, 0));
        }
        else
        {
            Left.Compile(instructions, false);
            Right.Compile(instructions, false);
        }
    }
}

public class DivNode : INode
{
    public INode Left { get; init; }
    public INode Right { get; init; }

    public double Evaluate(Context ctx)
    {
        return Left.Evaluate(ctx) / Right.Evaluate(ctx);
    }

    public NodeClosure Closure()
    {
        var left = Left.Closure();
        var right = Right.Closure();
        return ctx => left(ctx) / right(ctx);
    }

    public void Compile(List<Instruction> instructions, bool useValue)
    {
        if (useValue)
        {
            Left.Compile(instructions, true);
            Right.Compile(instructions, true);
            instructions.Add(new Instruction(OpCode.DIV, 0));
        }
        else
        {
            Left.Compile(instructions, false);
            Right.Compile(instructions, false);
        }
    }
}

public class ModNode : INode
{
    public INode Left { get; init; }
    public INode Right { get; init; }

    public double Evaluate(Context ctx)
    {
        return Left.Evaluate(ctx) % Right.Evaluate(ctx);
    }

    public NodeClosure Closure()
    {
        var left = Left.Closure();
        var right = Right.Closure();
        return ctx => left(ctx) % right(ctx);
    }

    public void Compile(List<Instruction> instructions, bool useValue)
    {
        if (useValue)
        {
            Left.Compile(instructions, true);
            Right.Compile(instructions, true);
            instructions.Add(new Instruction(OpCode.MOD, 0));
        }
        else
        {
            Left.Compile(instructions, false);
            Right.Compile(instructions, false);
        }
    }
}

public class EqNode : INode
{
    public INode Left { get; init; }
    public INode Right { get; init; }

    public double Evaluate(Context ctx)
    {
        return Left.Evaluate(ctx) == Right.Evaluate(ctx) ? 1 : 0;
    }

    public NodeClosure Closure()
    {
        var left = Left.Closure();
        var right = Right.Closure();
        return ctx => left(ctx) == right(ctx) ? 1 : 0;
    }

    public void Compile(List<Instruction> instructions, bool useValue)
    {
        if (useValue)
        {
            Left.Compile(instructions, true);
            Right.Compile(instructions, true);
            instructions.Add(new Instruction(OpCode.EQ, 0));
        }
        else
        {
            Left.Compile(instructions, false);
            Right.Compile(instructions, false);
        }
    }
}

public class NeqNode : INode
{
    public INode Left { get; init; }
    public INode Right { get; init; }

    public double Evaluate(Context ctx)
    {
        return Left.Evaluate(ctx) != Right.Evaluate(ctx) ? 1 : 0;
    }

    public NodeClosure Closure()
    {
        var left = Left.Closure();
        var right = Right.Closure();
        return ctx => left(ctx) != right(ctx) ? 1 : 0;
    }

    public void Compile(List<Instruction> instructions, bool useValue)
    {
        if (useValue)
        {
            Left.Compile(instructions, true);
            Right.Compile(instructions, true);
            instructions.Add(new Instruction(OpCode.NEQ, 0));
        }
        else
        {
            Left.Compile(instructions, false);
            Right.Compile(instructions, false);
        }
    }
}

public class LtNode : INode
{
    public INode Left { get; init; }
    public INode Right { get; init; }

    public double Evaluate(Context ctx)
    {
        return Left.Evaluate(ctx) < Right.Evaluate(ctx) ? 1 : 0;
    }

    public NodeClosure Closure()
    {
        var left = Left.Closure();
        var right = Right.Closure();
        return ctx => left(ctx) < right(ctx) ? 1 : 0;
    }

    public void Compile(List<Instruction> instructions, bool useValue)
    {
        if (useValue)
        {
            Left.Compile(instructions, true);
            Right.Compile(instructions, true);
            instructions.Add(new Instruction(OpCode.LT, 0));
        }
        else
        {
            Left.Compile(instructions, false);
            Right.Compile(instructions, false);
        }
    }
}

public class GtNode : INode
{
    public INode Left { get; init; }
    public INode Right { get; init; }

    public double Evaluate(Context ctx)
    {
        return Left.Evaluate(ctx) > Right.Evaluate(ctx) ? 1 : 0;
    }

    public NodeClosure Closure()
    {
        var left = Left.Closure();
        var right = Right.Closure();
        return ctx => left(ctx) > right(ctx) ? 1 : 0;
    }

    public void Compile(List<Instruction> instructions, bool useValue)
    {
        if (useValue)
        {
            Left.Compile(instructions, true);
            Right.Compile(instructions, true);
            instructions.Add(new Instruction(OpCode.GT, 0));
        }
        else
        {
            Left.Compile(instructions, false);
            Right.Compile(instructions, false);
        }
    }
}

public class LteNode : INode
{
    public INode Left { get; init; }
    public INode Right { get; init; }

    public double Evaluate(Context ctx)
    {
        return Left.Evaluate(ctx) <= Right.Evaluate(ctx) ? 1 : 0;
    }

    public NodeClosure Closure()
    {
        var left = Left.Closure();
        var right = Right.Closure();
        return ctx => left(ctx) <= right(ctx) ? 1 : 0;
    }

    public void Compile(List<Instruction> instructions, bool useValue)
    {
        if (useValue)
        {
            Left.Compile(instructions, true);
            Right.Compile(instructions, true);
            instructions.Add(new Instruction(OpCode.LTE, 0));
        }
        else
        {
            Left.Compile(instructions, false);
            Right.Compile(instructions, false);
        }
    }
}

public class GteNode : INode
{
    public INode Left { get; init; }
    public INode Right { get; init; }

    public double Evaluate(Context ctx)
    {
        return Left.Evaluate(ctx) >= Right.Evaluate(ctx) ? 1 : 0;
    }

    public NodeClosure Closure()
    {
        var left = Left.Closure();
        var right = Right.Closure();
        return ctx => left(ctx) >= right(ctx) ? 1 : 0;
    }

    public void Compile(List<Instruction> instructions, bool useValue)
    {
        if (useValue)
        {
            Left.Compile(instructions, true);
            Right.Compile(instructions, true);
            instructions.Add(new Instruction(OpCode.GTE, 0));
        }
        else
        {
            Left.Compile(instructions, false);
            Right.Compile(instructions, false);
        }
    }
}

public static class Nodes
{
    public static INode Execute(params INode[] nodes) => new ExecuteNode { Nodes = nodes };

    public static INode If(INode condition, INode trueNode, INode falseNode) => new IfNode
        { Condition = condition, True = trueNode, False = falseNode };

    public static INode While(INode condition, INode body) => new WhileNode { Condition = condition, Body = body };
    public static INode Get(INode index) => new GetNode { Index = index };
    public static INode Set(INode index, INode value) => new SetNode { Index = index, Value = value };
    public static INode Add(INode left, INode right) => new AddNode { Left = left, Right = right };
    public static INode Sub(INode left, INode right) => new SubNode { Left = left, Right = right };
    public static INode Mul(INode left, INode right) => new MulNode { Left = left, Right = right };
    public static INode Div(INode left, INode right) => new DivNode { Left = left, Right = right };
    public static INode Mod(INode left, INode right) => new ModNode { Left = left, Right = right };

    public static INode Eq(INode left, INode right) => new EqNode { Left = left, Right = right };
    public static INode Neq(INode left, INode right) => new NeqNode { Left = left, Right = right };
    public static INode Lt(INode left, INode right) => new LtNode { Left = left, Right = right };
    public static INode Gt(INode left, INode right) => new GtNode { Left = left, Right = right };
    public static INode Lte(INode left, INode right) => new LteNode { Left = left, Right = right };
    public static INode Gte(INode left, INode right) => new GteNode { Left = left, Right = right };

    public static INode Node(this double value) => new ValueNode { Value = value };
    public static INode Node(this int value) => new ValueNode { Value = value };
}