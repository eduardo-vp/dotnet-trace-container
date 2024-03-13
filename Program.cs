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
    string? volumeMountPath = Environment.GetEnvironmentVariable("VOLUME_MOUNT_PATH");

    if (volumeMountPath == null)
    {
        return "Volume mount path is not specified. Please specify VOLUME_MOUNT_PATH environment variable";
    }

    if (!Directory.Exists(volumeMountPath))
    {
        return "Invalid volume mount path. Directory doesn't exist at the specified path";
    }

    return RunCommand("ls -l " + volumeMountPath);
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

    string? volumeMountPath = Environment.GetEnvironmentVariable("VOLUME_MOUNT_PATH");

    if (volumeMountPath == null)
    {
        return "Volume mount path is not specified. Please specify VOLUME_MOUNT_PATH environment variable";
    }

    if (!Directory.Exists(volumeMountPath))
    {
        return "Invalid volume mount path. Directory doesn't exist at the specified path";
    }

    string? output_path = null;
    string[] tokens = command.Split(" ");
    for (int i = 1; i < tokens.Length; ++i)
    {
        if (tokens[i - 1].Equals("--output") || tokens[i - 1].Equals("-o"))
        {
            tokens[i] = Path.Combine(volumeMountPath, tokens[i]);
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

    command = string.Join(" ", tokens);

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
    string volumeMountPath = Environment.GetEnvironmentVariable("VOLUME_MOUNT_PATH");
    file = Path.Combine(volumeMountPath, file);
    return Results.File(file);
});

// curl -X DELETE http://<host>:5000/delete/<file>
app.MapDelete("/delete/{file}", (HttpRequest request) =>
{
    string file = (string)request.RouteValues["file"];
    string volumeMountPath = Environment.GetEnvironmentVariable("VOLUME_MOUNT_PATH");
    file = Path.Combine(volumeMountPath, file);
    try
    {
        File.Delete(file);
        return "File deleted successfully";
    }
    catch (Exception e)
    {
        return $"Failed to delete file: {e.Message}";
    }
});

app.Run();

public class RequestBody
{
    public string Command { get; set; }
}
