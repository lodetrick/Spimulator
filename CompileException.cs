using System;

public class CompileException : Exception {

    private int line = 0;

    public int Line() {
        return line;
    }
    public CompileException()
    {
    }

    public CompileException(int line)
    {
        this.line = line;
    }

    public CompileException(string message) : base(message)
    {
    }

    public CompileException(string message, int line)
        : base(message)
    {
        this.line = line;
    }

    public CompileException(string message, int line, Exception inner)
        : base(message, inner)
    {
        this.line = line;
    }

    public CompileException(int line, Exception inner) :base("",inner)
    {
        this.line = line;
    }

    public CompileException(string message, Exception inner)
        : base(message, inner)
    {
    }
}