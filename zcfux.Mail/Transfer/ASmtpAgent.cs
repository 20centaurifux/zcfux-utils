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
namespace zcfux.Mail.Transfer;

public abstract class ASmtpAgent : IAgent
{
    readonly ISmtpOptions _options;

    protected ASmtpAgent(ISmtpOptions options)
        => _options = options;

    public void Transfer(IEmail message)
    {
        ConnectIfNecessary();
        Send(message);
    }

    void ConnectIfNecessary()
    {
        try
        {
            if (!IsConnected())
            {
                Connect(_options);
            }
        }
        catch (Exception ex)
        {
            throw new TransferException(
                $"Couldn't connect to `{_options.Host}:{_options.Port}'.",
                ex);
        }
    }

    protected abstract bool IsConnected();

    protected abstract void Connect(ISmtpOptions options);

    protected abstract void Send(IEmail email);
}