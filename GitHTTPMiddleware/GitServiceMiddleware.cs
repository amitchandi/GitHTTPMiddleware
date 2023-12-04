using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;

namespace GitHTTPMiddleware
{
    public class GitServiceMiddleware
    {

        private static readonly Dictionary<string, string> info_lengths = new Dictionary<string, string>()
        {
            { "git-upload-pack", "001e" },
            { "git-receive-pack", "001f" }
        };

        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;

        public GitServiceMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var request = context.Request;
            var response = context.Response;

            var path = request.Path;

            if (request.Headers["User-Agent"].ToString().Contains("git"))
            {
                var segments = path.ToString().Split('/');

                if (segments == null || segments.Length < 3)
                    throw new InvalidRequestException("Bad URL");

                var user = segments[1];
                var repo_name = segments[2];

                var action = segments[segments.Length - 1];

                var service = request.Query["service"].ToString();

                if (service == null || service == "")
                {
                    service = action;
                }

                if (service != "git-upload-pack" && service != "git-receive-pack")
                {
                    throw new InvalidRequestException("Invalid Service.");
                }

                bool info = false;

                if (segments[3] == "info")
                {
                    info = true;
                }

                var typeExtension = info ? "advertisement" : "result";
                var git_response_header = $"application/x-{service}-{typeExtension}";

                response.StatusCode = (int)HttpStatusCode.OK;
                response.ContentType = git_response_header;
                response.Headers.Add("Cache-Control", "no-cache");

                var repositories_dir = _configuration["Git:repositories_dir"];
                Console.WriteLine(repositories_dir);
                await ExecuteGitCmd(info, service, $@"{repositories_dir}\{user}\{repo_name}", request.Body, response.Body);

                response.Body.Close();
            }
            else
            {
                await _next(context);
            }

        }

        private async Task ExecuteGitCmd(bool info, string service, string workingDirectory, Stream inStream, Stream outStream)
        {
            var git_binary_path = _configuration["Git:git_binary_path"];

            var psi = new ProcessStartInfo($@"{git_binary_path}\{service}.exe")
            {
                Arguments = $"--stateless-rpc " + (info ? "--advertise-refs" : "") + " ./",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                WorkingDirectory = workingDirectory,
            };

            var proc = Process.Start(psi) ?? throw new Exception("Error running process.");

            using (proc)
            {
                if (info)
                {
                    await WriteToStream(outStream, $"{info_lengths[service]}# service={service}\n");
                    await WriteToStream(outStream, "0000");
                    await proc.StandardOutput.BaseStream.CopyToAsync(outStream);
                }
                else
                {
                    await inStream.CopyToAsync(proc.StandardInput.BaseStream);
                    proc.StandardInput.Close();
                    await proc.StandardOutput.BaseStream.CopyToAsync(outStream);
                }

                proc.WaitForExit();
            }

        }

        private static async Task WriteToStream(Stream stream, string data)
        {
            var buffer = Encoding.UTF8.GetBytes(data);
            await stream.WriteAsync(buffer, 0, buffer.Length);
        }
    }

    public static class GitServiceMiddlewareExtensions
    {
        public static IApplicationBuilder UseGitService(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GitServiceMiddleware>();
        }
    }

    public class InvalidRequestException : Exception
    {
        public InvalidRequestException()
        {
        }

        public InvalidRequestException(string message)
            : base(message)
        {
        }

        public InvalidRequestException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
