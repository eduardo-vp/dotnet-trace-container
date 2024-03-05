using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

string RunCommand(string command)
{
    var process = new Process()
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = "/bin/sh",
            Arguments = $"-c \"{command}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        }
    };
    process.Start();
    string output = "";
    while (process.StandardOutput.Peek() > -1) output += process.StandardOutput.ReadLine() + Environment.NewLine;
    while (process.StandardError.Peek() > -1) output += process.StandardError.ReadLine() + Environment.NewLine;
    process.WaitForExit();
    return output; 
}

// curl http://<host>:5000/ps
app.MapGet("/ps", () =>
{
    return RunCommand("dotnet-trace ps");
});

// curl http://<host>:5000/ls
app.MapGet("/ls", () =>
{
    return RunCommand("ls");
});

// curl -X POST -H "Content-Type: application/json" -d '{"Command": "dotnet-trace collect -p <pid> -o <path> --profile gc-collect --duration <in hh:mm:ss format>"}' http://<host>:5000/capture-trace
app.MapPost("/capture-trace", ([FromBody] RequestBody request) =>
{
    var command = request.Command;
    if (!command.StartsWith("dotnet-trace collect"))
    {
        return "Invalid command. Command should start with dotnet-trace collect";
    }

    if (!(command.Contains("--output") || command.Contains("-o")))
    {
        return "Output file is not specified. Please specify --output <output path with .nettrace extension> option";
    }

    if (!command.Contains("-p"))
    {
        return "Process id is not specified. Please specify -p <pid> option";
    }

    string? output_path = null;
    string[] tokens = command.Split(" ");
    for (int i = 1; i < tokens.Length; ++i)
    {
        if (tokens[i - 1].Equals("--output") || tokens[i - 1].Equals("-o"))
        {
            output_path = tokens[i];
            if (!output_path.EndsWith(".nettrace"))
            {
                return "Invalid output file. Output file should have .nettrace extension";
            }
            break;
        }
    }

    if (output_path == null)
    {
        return "Output file is not specified. Please specify --output <output path with .nettrace extension> option";
    }

    var process = new Process()
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = "/bin/sh",
            Arguments = $"-c \"{command}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        }
    };
    string response;
    try
    {
        process.Start();
        string process_output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        response = process_output;
    }
    catch (Exception e)
    {
        response = e.Message;
    }
    return response;
});


// wget http://<host>:5000/download/<file>
app.MapGet("/download/{file}", (HttpRequest request) =>
{
    string file = (string) request.RouteValues["file"];
    file = "/" + file;
    return Results.File(file);
});

app.Run();

public class RequestBody
{
    public string Command { get; set; }
}