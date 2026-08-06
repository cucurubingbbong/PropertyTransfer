using System;
using UnityEngine;

/// <summary>
/// 디버그 명령어 클래스
/// </summary>
public class DebugCommand
{
    public string Name {get;}

    public string Description {get;}

    public string Usage {get;}

    private readonly Action<string[]> execute;

    public DebugCommand(string name, string description, string usage, Action<string[]> execute)
    {
        Name = name;
        Description = description;
        Usage = usage;
        this.execute = execute;
    }

    public void Execute(string[] args)
    {
        execute?.Invoke(args);
    }
}
