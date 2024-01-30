using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

public enum CompilerStage {
    Empty,
    Data,
    Text,
}
public struct Command {
    public string mnemonic {get;}
    public uint binary {get;}
    public int[] argshamt {get;}

    public Command() {
        mnemonic = "";
        binary = 0xFFFF_FFFF;
        argshamt = new int[1];
    }
    public Command(string mnemonic, uint binary, int[] argshamt) {
        this.mnemonic = mnemonic;
        this.binary = binary;
        this.argshamt = argshamt;
    }

    //just for nice initializing
    public static implicit operator Command(ValueTuple<string,uint,int[]> tuple) {
        return new Command(tuple.Item1, tuple.Item2, tuple.Item3);
    }
}

public static class Compiler {
    public static readonly string[] register_names = {"$zero","$at","$v0","$v1",
                                                      "$a0","$a1","$a2","$a3",
                                                      "$t0","$t1","$t2","$t3",
                                                      "$t4","$t5","$t6","$t7",
                                                      "$s0","$s1","$s2","$s3",
                                                      "$s4","$s5","$s6","$s7",
                                                      "$t8","$t9","$k0","$k1",
                                                      "$gp","$sp","$fp","$ra"};

    public static readonly Command[] commands = {("addi",   0x20000000,new int[]{16,21,0 }),
                                                 ("add",    0x00000020,new int[]{11,21,16}),
                                                 ("sub",    0x00000022,new int[]{11,21,16}),
                                                 ("and",    0x00000024,new int[]{11,21,16}),
                                                 ("lui",    0x3C000000,new int[]{16,0    }),
                                                 ("sll",    0x00000000,new int[]{11,16,6 }),
                                                 ("srl",    0x00000002,new int[]{11,16,6 }),
                                                 ("sra",    0x00000003,new int[]{11,16,6 }),
                                                 ("syscall",0x0000000C,new int[]{        }),
                                                 ("ori",    0x34000000,new int[]{16,21,0 }),
                                                 ("addu",   0x00000021,new int[]{11,21,16}),
                                                 ("or",     0x00000025,new int[]{11,21,16}),
                                                 ("nor",    0x00000027,new int[]{11,21,16}),
                                                 ("addiu",  0x24000000,new int[]{16,21,0 }),
                                                 ("slt",    0x0000002A,new int[]{11,21,16}),
                                                 ("andi",   0x30000000,new int[]{16,21,0 })};

    public static uint[] Compile(string input) {
        List<uint> instructions = new List<uint>();
        List<ValueTuple<string,int>> functions = new List<ValueTuple<string,int>>();

        List<string> line_by_line = input.Split("\n").Select(inp => inp.Trim()).ToList();

        //Preprocessing: Turn Pseudoinstructions into their proper instructions
        
        for (int i = 0; i < line_by_line.Count; i++) {
            try {
                if (Regex.IsMatch(line_by_line[i],"^li\\s")) { //looks for "li " at the start of the string
                    //  li $rd 0x12345678
                    //  goes to:

                    //  lui $at {upper half}
                    //  ori {$rd} {lower half}
                    string[] dissected = ProperSplit(line_by_line[i]); //should have 3 parts

                    int immediate = NumFromText(dissected[2]);
                    uint low  = (uint)(0x0000FFFF & (immediate));
                    uint high = (uint)(0x0000FFFF & (immediate >> 16));

                    line_by_line.RemoveAt(i);
                    if (high == 0) {
                        line_by_line.Insert(i,$"addi {dissected[1]} $0, 0x{low.ToString("X8")}");
                    }
                    else {
                        line_by_line.Insert(i,$"ori {dissected[1]} $at, 0x{low.ToString("X8")}");
                        line_by_line.Insert(i,$"lui $at, 0x{high.ToString("X8")}");
                        i++;
                    }
                }
                else if (Regex.IsMatch(line_by_line[i],"^move\\s")) {
                    string[] dissected = ProperSplit(line_by_line[i]); //3 parts move $rd, $rs

                    line_by_line.RemoveAt(i);
                    line_by_line.Insert(i,$"add {dissected[1]} {dissected[2]} $0");
                }


            }
            catch (Exception e) {
                throw new CompileException(i,e);
            }
            
        }

        //End Preprocessing
        
        CompilerStage currentstage = CompilerStage.Empty;

        for (int i = 0; i < line_by_line.Count; i++) {
            //need to check for the fluff, and sort that out
            //need to remember where functions are in the binary, to aid in jumping to them
            if (line_by_line[i].StartsWith('#')) {
                //comment, ignore
            }
            else if (line_by_line[i].StartsWith('.')) { // .text or .data
                if (line_by_line[i].Equals(".text")) {
                    currentstage = CompilerStage.Text;
                }
                else if (line_by_line[i].Equals(".data")) {
                    currentstage = CompilerStage.Data;
                }
                else {
                    throw new CompileException(i,new CompileException($"Unidentified Header: {line_by_line[i]}"));
                }
            }
            else if (currentstage == CompilerStage.Text) {
                if (line_by_line[i].EndsWith(':')) { // function
                    functions.Add((line_by_line[i].Substring(0,line_by_line[i].Length-2),instructions.Count));
                }
                else { // instruction
                    try {
                        instructions.Add(CompileLine(line_by_line[i]));
                    }
                    catch (Exception e) {
                        throw new CompileException(i,e);
                    }
                }
            }
            else if (currentstage == CompilerStage.Data) {

            }
        }

        return instructions.ToArray();
    }

    public static uint CompileLine(string input) {
        uint instruction;
        //split the instruction by spaces
        //examine regex.split
        string[] tokens = ProperSplit(input);
        //identify what type the input is
        //call a function that builds the uint based on the tokens

        Command function = CommandFromName(tokens[0]);
        instruction = function.binary;

        for (uint i = 0; i < function.argshamt.Length; i++) {
            if (tokens[i + 1][0] == '$') { // register
                uint index = RegIndexFromName(tokens[i + 1].TrimEnd(','));
                instruction |= (0x1F & index) << function.argshamt[i];
            }
            else { //immediate
                //need to check if immediate vs addressing like 4($t0)
                int num = 0;
                try {
                    num = NumFromText(tokens[i+1]);
                }
                catch (Exception) {
                    throw new CompileException($"Could not Identify Number From {tokens[i+1]}");
                }
                

                if (function.argshamt[i] == 0) { //immediate
                    instruction |= (uint)(0x0000FFFF & num);
                } else { //shamt
                    instruction |= (uint)(0x0000001F & num) << function.argshamt[i];
                }
            }
        }
        return instruction;
    }

    public static int NumFromText(string token) {
        int sign = token.StartsWith("-") ? 1 : 0;
        if (Regex.IsMatch(token,"^0x|^-0x")) { //if number is hexadecimal
            return (1 - 2 * sign) * Convert.ToInt32(token.Substring(sign),16);
        }
        else if (Regex.IsMatch(token,"^0b|^-0b")) { //if number is binary
            return (1 - 2 * sign) * Convert.ToInt32(token.Substring(2 + sign),2);
        }
        else { //if number is normal
            return Convert.ToInt32(token);
        }
    }

    public static uint RegIndexFromName(string name) {
        if (Regex.IsMatch(name,"^\\$\\d")) { //second character is number
            uint index = 0;
            if (!uint.TryParse(name.Substring(1), out index)) {
                throw new CompileException($"Unidentified Register Format: {name}");
            }
            if (index > 31 || index < 0) {
                throw new CompileException($"Register {index} Out of Range");
            }
            return index;
        }
        for (uint i = 0; i < 32; i++) {
            if (name.Equals(register_names[i])) {
                return i;
            }
        }
        throw new CompileException($"Register {name} Doesn't Exist");
    }

    public static Command CommandFromName(string name) {
        for (uint i = 0; i < commands.Length; i++) {
            if (name.Equals(commands[i].mnemonic)) {
                return commands[i];
            }
        }
        throw new CompileException($"Unidentified Command: {name}");
    }

    public static Command CommandFromBinary(uint binary) {
        for (uint i = 0; i < commands.Length; i++) {
            if (commands[i].binary == binary) {
                return commands[i];
            }
        }
        throw new CompileException();
    }

    public static string[] ProperSplit(string to_split) {
        return to_split.Split(" ").Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
    }
}