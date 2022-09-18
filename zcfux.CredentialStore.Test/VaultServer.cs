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

namespace zcfux.CredentialStore.Test;

public sealed class VaultServer
{
    [DllImport("libc", SetLastError = true)]
    static extern int kill(int pid, int sig);

    Process? _process;
    readonly AutoResetEvent _started = new(initialState: false);

    public void Start()
    {
        _process = new Process()
        {
            EnableRaisingEvents = true
        };

        var executable = Environment.GetEnvironmentVariable("VAULT_TEST_EXE")
                         ?? "vault";

        var url = Environment.GetEnvironmentVariable("VAULT_TEST_ADDRESS")
                  ?? "127.0.0.1:8200";

        _process.StartInfo.FileName = executable;
        _process.StartInfo.UseShellExecute = false;
        _process.StartInfo.Arguments = $"server -dev -dev-root-token-id=root -dev-listen-address {url}";

        _process.Start();

        if (_process.WaitForExit(2500))
        {
            throw new Exception("Couldn't start Vault server.");
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