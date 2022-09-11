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
using zcfux.Mail.Transfer;

namespace zcfux.Mail.Test;

internal sealed class SmtpServer
{
    static readonly string TempDir = Path.Combine(
        Path.GetTempPath(),
        typeof(ASmtpTests).FullName!);

    Process? _process;

    public void Start(ESecureSocketOptions secureSocketOptions, int port)
    {
        CreateTempDir();
        StartProcess(secureSocketOptions, port);
        WaitUntilInitialized();
    }

    static void CreateTempDir()
        => Directory.CreateDirectory(TempDir);

    void StartProcess(ESecureSocketOptions secureSocketOptions, int port)
    {
        _process = new Process();

        var executable = Environment.GetEnvironmentVariable("SMTP4DEV_EXE")
                         ?? "smtp4dev";

        _process.StartInfo.FileName = executable;

        var args = BuildArgs(secureSocketOptions, port);

        _process.StartInfo.Arguments = string.Join(" ", args);

        _process.StartInfo.RedirectStandardOutput = true;
        
        _process.Start();
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
        
        return args.ToArray();
    }

    void WaitUntilInitialized()
    {
        var initialized = false;

        while (!initialized)
        {
            var line = _process?.StandardOutput.ReadLine();
            
            initialized = line?.StartsWith("Application started") ?? false;
        }
    }

    public void WaitUntilProcessed()
    {
        var processed = false;

        while (!processed)
        {
            var line = _process?.StandardOutput.ReadLine();
            
            processed = line?.StartsWith("Processing received message DONE") ?? false;
        }  
    }

    public void Stop()
    {
        if (_process != null)
        {
            _process.Kill();
            _process.WaitForExit();
        }
    }
}