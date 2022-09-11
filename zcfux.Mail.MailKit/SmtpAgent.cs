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
using System.Security.Cryptography.X509Certificates;
using zcfux.Mail.Transfer;
using Smtp = MailKit.Net.Smtp;
using Security = MailKit.Security;
using MimeKit;

namespace zcfux.Mail.MailKit;

public sealed class SmtpAgent : ASmtpAgent
{
    readonly Smtp.ISmtpClient _client;

    public SmtpAgent(ISmtpOptions options)
        : base(options)
    {
        _client = new Smtp.SmtpClient();

        if (options.ClientCertificates.Any())
        {
            _client.ClientCertificates = new X509CertificateCollection();

            _client.ClientCertificates.AddRange(options.ClientCertificates);
        }

        _client.ServerCertificateValidationCallback = options.ServerCertificateValidationCallback;
    }

    protected override bool IsConnected()
    {
        var connected = false;

        if (_client.IsConnected)
        {
            try
            {
                _client.NoOp();

                connected = true;
            }
            catch
            {
                try
                {
                    _client.Disconnect(quit: false);
                }
                catch
                {
                    // <(^.^)>
                }
            }
        }

        return connected;
    }

    protected override void Connect(ISmtpOptions options)
    {
        var secureSocketOptions = MapSecureSocketOptions(options.SecureSocketOptions);

        _client.Connect(
            options.Host,
            options.Port,
            secureSocketOptions);

        if (!string.IsNullOrEmpty(options.Username))
        {
            _client.Authenticate(options.Username, options.Password ?? string.Empty);
        }
    }

    static Security.SecureSocketOptions MapSecureSocketOptions(ESecureSocketOptions options)
        => options switch
        {
            ESecureSocketOptions.ImplicitTls => Security.SecureSocketOptions.SslOnConnect,
            ESecureSocketOptions.StartTls => Security.SecureSocketOptions.StartTls,
            _ => Security.SecureSocketOptions.None
        };

    protected override void Send(IEmail email)
    {
        var message = ConvertMessage(email);

        _client.Send(message);
    }

    protected override void Disconnect()
        => _client?.Disconnect(quit: true);

    static MimeMessage ConvertMessage(IEmail email)
    {
        var message = new MimeMessage();

        message.From.Add(email.From.ToMailboxAddress());
        message.Subject = email.Subject;

        AddReceivers(message, email);
        AddBody(message, email);

        return message;
    }

    static void AddReceivers(MimeMessage mimeMessage, IEmail email)
    {
        mimeMessage.To.AddRange(
            email.To.Select(addr => addr.ToMailboxAddress()));

        mimeMessage.Cc.AddRange(
            email.Cc.Select(addr => addr.ToMailboxAddress()));

        mimeMessage.Bcc.AddRange(
            email.Bcc.Select(addr => addr.ToMailboxAddress()));
    }

    static void AddBody(MimeMessage mimeMessage, IEmail email)
    {
        var builder = new BodyBuilder();

        if (email.TextBody != null)
        {
            builder.TextBody = email.TextBody;
        }

        if (email.HtmlBody != null)
        {
            builder.HtmlBody = email.HtmlBody;
        }

        foreach (var attachment in email.GetAttachments().ToArray())
        {
            using (var stream = attachment.OpenRead())
            {
                var ms = new MemoryStream();

                stream.CopyTo(ms);

                builder.Attachments.Add(
                    attachment.Filename,
                    ms.ToArray());
            }
        }

        mimeMessage.Body = builder.ToMessageBody();
    }
}