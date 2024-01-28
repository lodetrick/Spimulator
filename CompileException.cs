using System;

public class CompileException : Exception {

    private int line = 0;
    public CompileException()
    {
    }

    public CompileException(string message, int line)
        : base(message)
    {
        this.line = line;
    }

    public CompileException(string message, Exception inner)
        : base(message, inner)
    {
    }
}