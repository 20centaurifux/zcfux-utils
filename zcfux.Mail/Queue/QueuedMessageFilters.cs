﻿/***************************************************************************
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
using zcfux.Filter;

namespace zcfux.Mail.Queue;

public static class QueuedMessageFilters
{
    public static Column<long> Id { get; } = Column<long>.FromMember();

    public static Column<int> QueueId { get; } = Column<int>.FromMember();

    public static Column<string> Queue { get; } = Column<string>.FromMember();

    public static Column<string> From { get; } = Column<string>.FromMember();

    public static Column<string> To { get; } = Column<string>.FromMember();

    public static Column<string> Cc { get; } = Column<string>.FromMember();

    public static Column<string> Bcc { get; } = Column<string>.FromMember();

    public static Column<string> Subject { get; } = Column<string>.FromMember();

    public static Column<string> TextBody { get; } = Column<string>.FromMember();

    public static Column<string> HtmlBody { get; } = Column<string>.FromMember();

    public static Column<long> AttachmentId { get; } = Column<long>.FromMember();

    public static Column<string> Attachment { get; } = Column<string>.FromMember();

    public static Column<DateTime> Created { get; } = Column<DateTime>.FromMember();

    public static Column<DateTime> EndOfLife { get; } = Column<DateTime>.FromMember();

    public static Column<DateTime?> NextDue { get; } = Column<DateTime?>.FromMember();

    public static Column<int> Errors { get; } = Column<int>.FromMember();
}