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
using LinqToDB.Mapping;

namespace zcfux.Mail.LinqToPg;

[Table(Schema = "mail", Name = "Message")]
sealed class MessageRelation : IMessage
{
#pragma warning disable CS8618
    public MessageRelation()
    {
    }

    public MessageRelation(IMessage message)
    {
        Id = message.Id;
        From = message.From.ToString();
        To = message.To.Select(addr => addr.ToString()).ToArray();
        Cc = message.Cc.Select(addr => addr.ToString()).ToArray();
        Bcc = message.Bcc.Select(addr => addr.ToString()).ToArray();
        Subject = message.Subject;
        TextBody = message.TextBody;
        HtmlBody = message.HtmlBody;
    }

    [Column(Name = "Id", IsPrimaryKey = true, IsIdentity = true)]
    public long? Id { get; set; }

    long IMessage.Id => Id!.Value;

    [Column(Name = "Sender", CanBeNull = false)]
    public string From { get; set; }

    Address IMessage.From => Address.FromString(From);

    [Column(Name = "To", CanBeNull = false)]
    public string[] To { get; set; }

    IEnumerable<Address> IMessage.To => To.Select(Address.FromString);

    [Column(Name = "Cc")]
    public string[] Cc { get; set; }

    IEnumerable<Address> IMessage.Cc => Cc.Select(Address.FromString);

    [Column(Name = "Bcc")]
    public string[] Bcc { get; set; }

    IEnumerable<Address> IMessage.Bcc => Bcc.Select(Address.FromString);

    [Column(Name = "Subject", CanBeNull = false)]
    public string Subject { get; set; }

    string IMessage.Subject => Subject;

    [Column(Name = "TextBody")]
    public string? TextBody { get; set; }

    string? IMessage.TextBody => TextBody;

    [Column(Name = "HtmlBody")]
    public string? HtmlBody { get; set; }

    string? IMessage.HtmlBody => HtmlBody;
#pragma warning restore CS8618
}