using System.Runtime.InteropServices;

namespace SonolusPerformanceTests;

public enum OpCode : byte
{
    NOOP, // No operation
    PUSH, // Push a constant value onto the stack
    POP, // Remove the top element from the stack
    JMP, // Jump to a instruction
    POP_JMP_IF_FALSE, // Pop the top element from the stack and jump to a instruction if it is false
    POP_JMP_IF_TRUE, // Pop the top element from the stack and jump to a instruction if it is true
    GET, // Get a variable from a constant location
    SET, // Set a variable to a constant location
    GET_INDIRECT, // Get a variable from a variable location (the top element of the stack)
    SET_INDIRECT, // Set a variable to a variable location (the top element of the stack)
    ADD, // Add the top two values on the stack
    SUB, // Subtract the top two values on the stack
    MUL, // Multiply the top two values on the stack
    DIV, // Divide the top two values on the stack
    MOD, // Modulo the top two values on the stack
    EQ, // Check if the top two values on the stack are equal
    NEQ, // Check if the top two values on the stack are not equal
    LT, // Check if the top two values on the stack are less than
    GT, // Check if the top two values on the stack are greater than
    LTE, // Check if the top two values on the stack are less than or equal to
    GTE, // Check if the top two values on the stack are greater than or equal to
    AND, // Check if the top two values on the stack are both true
    OR, // Check if the top two values on the stack are either true
    NOT, // Check if the top value on the stack is false
}

[StructLayout(LayoutKind.Explicit)]
public struct Instruction
{
    [FieldOffset(0)] public OpCode OpCode;

    [FieldOffset(1)] public double DoubleValue;

    [FieldOffset(1)] public int IntValue;

    public Instruction(OpCode opCode, double value)
    {
        OpCode = opCode;
        IntValue = 0;
        DoubleValue = value;
    }

    public Instruction(OpCode opCode, int value)
    {
        OpCode = opCode;
        DoubleValue = 0;
        IntValue = value;
    }

    public static readonly Instruction NOOP = new Instruction(OpCode.NOOP, 0);

    public override string ToString()
    {
        var integerDataOpCodes = new[]
        {
            OpCode.JMP,
            OpCode.POP_JMP_IF_FALSE,
            OpCode.POP_JMP_IF_TRUE,
            OpCode.GET,
            OpCode.SET,
        };
        var doubleDataOpCodes = new[]
        {
            OpCode.PUSH,
        };
        if (integerDataOpCodes.Contains(OpCode))
        {
            return $"{OpCode} {IntValue}";
        }
        if (doubleDataOpCodes.Contains(OpCode))
        {
            return $"{OpCode} {DoubleValue}";
        }
        return $"{OpCode}";
    }

    public static Instruction[] Compile(INode node)
    {
        var instructions = new List<Instruction>();
        node.Compile(instructions, true);
        return instructions.ToArray();
    }
}

static class BytecodeInterpreter
{
    public static double Run(Instruction[] instructions, Context ctx)
    {
        int ip = 0;
        double[] stack = new double[1024];
        int sp = 0;

        while (ip < instructions.Length)
        {
            var instruction = instructions[ip];
            switch (instruction.OpCode)
            {
                case OpCode.NOOP:
                    break;
                case OpCode.PUSH:
                    stack[sp++] = instruction.DoubleValue;
                    break;
                case OpCode.POP:
                    sp--;
                    break;
                case OpCode.JMP:
                    ip = instruction.IntValue;
                    continue;
                case OpCode.POP_JMP_IF_FALSE:
                    if (stack[--sp] == 0)
                    {
                        ip = instruction.IntValue;
                        continue;
                    }
                    break;
                case OpCode.POP_JMP_IF_TRUE:
                    if (stack[--sp] != 0)
                    {
                        ip = instruction.IntValue;
                        continue;
                    }
                    break;
                case OpCode.GET:
                    stack[sp++] = ctx.Memory[instruction.IntValue];
                    break;
                case OpCode.SET:
                    ctx.Memory[instruction.IntValue] = stack[--sp];
                    break;
                case OpCode.GET_INDIRECT:
                    var address = (int)stack[--sp];
                    stack[sp++] = ctx.Memory[address];
                    break;
                case OpCode.SET_INDIRECT:
                    var value = stack[--sp];
                    address = (int)stack[--sp];
                    ctx.Memory[address] = value;
                    break;
                case OpCode.ADD:
                    stack[sp - 2] += stack[sp - 1];
                    sp--;
                    break;
                case OpCode.SUB:
                    stack[sp - 2] -= stack[sp - 1];
                    sp--;
                    break;
                case OpCode.MUL:
                    stack[sp - 2] *= stack[sp - 1];
                    sp--;
                    break;
                case OpCode.DIV:
                    stack[sp - 2] /= stack[sp - 1];
                    sp--;
                    break;
                case OpCode.MOD:
                    stack[sp - 2] %= stack[sp - 1];
                    sp--;
                    break;
                case OpCode.EQ:
                    stack[sp - 2] = stack[sp - 2] == stack[sp - 1] ? 1 : 0;
                    sp--;
                    break;
                case OpCode.NEQ:
                    stack[sp - 2] = stack[sp - 2] != stack[sp - 1] ? 1 : 0;
                    sp--;
                    break;
                case OpCode.LT:
                    stack[sp - 2] = stack[sp - 2] < stack[sp - 1] ? 1 : 0;
                    sp--;
                    break;
                case OpCode.GT:
                    stack[sp - 2] = stack[sp - 2] > stack[sp - 1] ? 1 : 0;
                    sp--;
                    break;
                case OpCode.LTE:
                    stack[sp - 2] = stack[sp - 2] <= stack[sp - 1] ? 1 : 0;
                    sp--;
                    break;
                case OpCode.GTE:
                    stack[sp - 2] = stack[sp - 2] >= stack[sp - 1] ? 1 : 0;
                    sp--;
                    break;
                case OpCode.AND:
                    stack[sp - 2] = stack[sp - 2] != 0 && stack[sp - 1] != 0 ? 1 : 0;
                    sp--;
                    break;
                case OpCode.OR:
                    stack[sp - 2] = stack[sp - 2] != 0 || stack[sp - 1] != 0 ? 1 : 0;
                    sp--;
                    break;
                case OpCode.NOT:
                    stack[sp - 1] = stack[sp - 1] == 0 ? 1 : 0;
                    break;
            }

            ip++;
        }

        if (sp > 0)
        {
            return stack[sp - 1];
        }

        return 0;
    }
}