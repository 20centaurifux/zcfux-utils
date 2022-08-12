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

namespace zcfux.Mail.LinqToPg.Queue;

[Table(Schema = "mail", Name = "QueuedMessages")]
internal class QueuedMessageView
{
#pragma warning disable CS8618
    [Column(Name = "Id", IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    [Column(Name = "QueueId")]
    public int QueueId { get; set; }

    [Column(Name = "Queue")]
    public string Queue { get; set; }

    [Column(Name = "Sender", CanBeNull = false)]
    public string From { get; set; }

    [Column(Name = "To", CanBeNull = false)]
    public string[] To { get; set; }

    [Column(Name = "Cc")]
    public string[] Cc { get; set; }

    [Column(Name = "Bcc")]
    public string[] Bcc { get; set; }

    [Column(Name = "Subject", CanBeNull = false)]
    public string Subject { get; set; }

    [Column(Name = "TextBody")]
    public string? TextBody { get; set; }

    [Column(Name = "HtmlBody")]
    public string? HtmlBody { get; set; }

    [Column(Name = "Created")]
    public DateTime Created { get; set; }

    [Column(Name = "EndOfLife")]
    public DateTime EndOfLife { get; set; }

    [Column(Name = "NextDue")]
    public DateTime? NextDue { get; set; }

    [Column(Name = "Errors")]
    public int Errors { get; set; }
#pragma warning restore CS8618
}