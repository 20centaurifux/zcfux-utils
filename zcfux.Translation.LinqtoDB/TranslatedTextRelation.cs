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

namespace zcfux.Translation.LinqToDB;

#pragma warning disable CS8618
[Table(Schema = "translation", Name = "TranslatedText")]
sealed class TranslatedTextRelation
{
    [Column(Name = "ResourceId"), PrimaryKey]
    public int ResourceId { get; set; }

    [Association(ThisKey = "ResourceId", OtherKey = "Id")]
    public TextResourceRelation Resource { get; set; }

    [Column(Name = "LocaleId"), PrimaryKey]
    public int LocaleId { get; set; }

    [Association(ThisKey = "LocaleId", OtherKey = "Id")]
    public LocaleRelation Locale { get; set; }

    [Column(Name = "Translation")]
    public string Translation { get; set; }
}
#pragma warning restore CS8618