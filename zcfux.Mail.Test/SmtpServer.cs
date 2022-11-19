/***************************************************************************
    begin........: December 2021
    copyright....: Sebastian Fedrau
    email........: sebastian.fedrau@gmail.com
 ***************************************************************************/

/***************************************************************************
    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU Lesser General Public
    License as published by the Free Software Foundation; either
    version 3 of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
    Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public License
    along with this program; if not, write to the Free Software Foundation,
    Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 ***************************************************************************/
using System.Diagnostics;
using System.Runtime.InteropServices;
using zcfux.Mail.Transfer;

namespace zcfux.Mail.Test;

internal sealed class SmtpServer
{
    [DllImport("libc", SetLastError = true)]
    static extern int kill(int pid, int sig);

    static readonly string TempDir = Path.Combine(
        Path.GetTempPath(),
        typeof(ASmtpTests).FullName!);

    Process? _process;
    readonly AutoResetEvent _started = new(initialState: false);
    readonly AutoResetEvent _received = new(initialState: false);

    public void Start(ESecureSocketOptions secureSocketOptions, int port)
    {
        CreateTempDir();
        StartProcess(secureSocketOptions, port);
        WaitUntilInitialized();
    }

    static void CreateTempDir()
    {
        if (Directory.Exists(TempDir))
        {
            Directory.Delete(TempDir, recursive: true);
        }

        Directory.CreateDirectory(TempDir);
    }

    void StartProcess(ESecureSocketOptions secureSocketOptions, int port)
    {
        _process = new Process()
        {
            EnableRaisingEvents = true
        };

        var executable = Environment.GetEnvironmentVariable("SMTP4DEV_EXE")
                         ?? "smtp4dev";

        var args = BuildArgs(secureSocketOptions, port);

        _process.StartInfo.FileName = executable;
        _process.StartInfo.WorkingDirectory = TempDir;
        _process.StartInfo.RedirectStandardOutput = true;
        _process.StartInfo.UseShellExecute = false;
        _process.StartInfo.Arguments = string.Join(" ", args);

        ReceiveStdout();

        _process.Start();
        _process.BeginOutputReadLine();
    }

    static string[] BuildArgs(ESecureSocketOptions secureSocketOptions, int port)
    {
        var url = Environment.GetEnvironmentVariable("SMTP4DEV_URL")
                  ?? "http://localhost:8000";

        var args = new List<string>
        {
            "--urls", url,
            "--hostname", "localhost",
            "--smtpport", port.ToString(),
            "--nousersettings",
            "--db", Path.Combine(TempDir, "smtp4dev.db"),
            "--recreatedb"
        };

        args.Add("--tlsmode");

        args.Add(secureSocketOptions switch
        {
            ESecureSocketOptions.ImplicitTls => "ImplicitTls",
            ESecureSocketOptions.StartTls => "StartTls",
            _ => "None"
        });

        var imapPort = Environment.GetEnvironmentVariable("SMTP4DEV_IMAP_PORT");

        if (imapPort != null)
        {
            args.Add("--imapport");
            args.Add(imapPort);
        }

        return args.ToArray();
    }

    void ReceiveStdout()
    {
        _process!.OutputDataReceived += (_, e) =>
        {
            if (e.Data is { } line)
            {
                if (line.StartsWith("Now listening on"))
                {
                    _started.Set();
                }

                if (line.StartsWith("Processing received message DONE"))
                {
                    _received.Set();
                }
            }
        };
    }

    void WaitUntilInitialized()
    {
        if (!_started.WaitOne(TimeSpan.FromSeconds(15)))
        {
            throw new TimeoutException();
        }
    }

    public void WaitUntilProcessed()
    {
        if (!_received.WaitOne(TimeSpan.FromSeconds(5)))
        {
            throw new TimeoutException();
        }
    }

    public void Stop()
    {
        if (_process != null)
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                kill(_process.Id, 15);
            }
            else
            {
                _process.Kill();
            }

            _process.WaitForExit();
        }
    }
}