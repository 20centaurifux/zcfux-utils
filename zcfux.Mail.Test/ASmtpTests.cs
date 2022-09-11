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
using NUnit.Framework;
using zcfux.Data;
using zcfux.Mail.Queue;
using zcfux.Mail.Transfer;

namespace zcfux.Mail.Test;

public abstract class ASmtpTests
{
    static readonly string TempDir = Path.Combine(
        Path.GetTempPath(),
        typeof(ASmtpTests).FullName!);

    IEngine _engine = null!;
    readonly SmtpServer _server = new();

    [SetUp]
    public void Setup()
    {
        _engine = CreateAndSetupEngine();

        Directory.CreateDirectory(TempDir);
    }

    [TearDown]
    public void Teardown()
    {
        Directory.Delete(TempDir, recursive: true);

        _server.Stop();
    }

    protected abstract IEngine CreateAndSetupEngine();

    protected abstract IDb CreateDb();

    protected abstract ASmtpAgent CreateSmtpAgent(ISmtpOptions options);

    QueueStore CreateQueueStore(object handle)
    {
        var db = CreateDb();
        var queueStore = new QueueStore(db, handle);

        return queueStore;
    }

    [Test]
    public void Send_NoTls()
        => SendAnonymously(ESecureSocketOptions.None);

    [Test]
    public void Send_NoTls_WithCredentials()
        => SendWithCredentials(ESecureSocketOptions.None);

    [Test]
    public void Send_StartTls()
        => SendAnonymously(ESecureSocketOptions.StartTls);

    [Test]
    public void Send_StartTls_WithCredentials()
        => SendWithCredentials(ESecureSocketOptions.StartTls);

    [Test]
    public void Send_Tls()
        => SendAnonymously(ESecureSocketOptions.StartTls);

    [Test]
    public void Send_Tls_WithCredentials()
        => SendWithCredentials(ESecureSocketOptions.StartTls);

    void SendAnonymously(ESecureSocketOptions secureSocketOptions)
    {
        var options = CreateSmtpOptionsBuilder(secureSocketOptions)
            .Build();

        if (StartSmtpServer())
        {
            _server.Start(options.SecureSocketOptions, options.Port);
        }

        var agent = CreateSmtpAgent(options);

        SendMessage(agent);
    }

    void SendWithCredentials(ESecureSocketOptions secureSocketOptions)
    {
        var builder = CreateSmtpOptionsBuilder(secureSocketOptions);

        builder = AddSmtpCredentials(builder);

        var options = builder.Build();

        if (StartSmtpServer())
        {
            _server.Start(options.SecureSocketOptions, options.Port);
        }

        var agent = CreateSmtpAgent(options);

        SendMessage(agent);
    }

    static bool StartSmtpServer()
    {
        var disabled = Environment.GetEnvironmentVariable("SMTP4DEV_DISABLED") ?? "0";

        return disabled.Equals("0");
    }

    static SmtpOptionsBuilder CreateSmtpOptionsBuilder(ESecureSocketOptions secureSocketOptions)
    {
        var builder = new SmtpOptionsBuilder()
            .WithHost(
                Environment.GetEnvironmentVariable("SMTP_TEST_HOST")
                ?? "localhost")
            .WithSecureSocketOptions(secureSocketOptions);

        var port = secureSocketOptions switch
        {
            ESecureSocketOptions.ImplicitTls
                => Environment.GetEnvironmentVariable("SMTP_TEST_TLS_PORT")
                   ?? "587",
            ESecureSocketOptions.StartTls
                => Environment.GetEnvironmentVariable("SMTP_TEST_STARTTLS_PORT")
                   ?? "25",
            _
                => Environment.GetEnvironmentVariable("SMTP_TEST_PORT")
                   ?? "25"
        };

        builder = builder.WithPort(int.Parse(port));

        if (secureSocketOptions != ESecureSocketOptions.None)
        {
            builder = builder.WithDisabledServerCertificateValidation();
        }

        return builder;
    }

    static SmtpOptionsBuilder AddSmtpCredentials(SmtpOptionsBuilder builder)
    {
        return builder
            .WithUsername(
                Environment.GetEnvironmentVariable("SMTP_TEST_USERNAME")
                ?? "alice")
            .WithPassword(
                Environment.GetEnvironmentVariable("SMTP_TEST_PASSWORD")
                ?? TestContext.CurrentContext.Random.GetString());
    }

    void SendMessage(IAgent agent)
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateQueueStore(t.Handle);

            var queue = store.NewQueue("a");

            var path = Path.Combine(TempDir, $"{Guid.NewGuid()}.txt");

            File.WriteAllText(path, TestContext.CurrentContext.Random.GetString());

            var message = queue.CreateMessageBuilder()
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject(TestContext.CurrentContext.Random.GetString())
                .WithTextBody(TestContext.CurrentContext.Random.GetString())
                .WithHtmlBody($"<p>{TestContext.CurrentContext.Random.GetString()}</p>")
                .WithAttachment(path)
                .Store();

            agent.Transfer(message);

            _server.WaitUntilProcessed();
        }
    }
}