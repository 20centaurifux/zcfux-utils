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
namespace zcfux.Translation.Data;

public interface ITranslationDb
{
    void WriteCategory(object handle, ICategory category);

    IEnumerable<ICategory> GetCategories(object handle);

    void DeleteCategory(object handle, int id);

    void WriteLocale(object handle, ILocale locale);

    IEnumerable<ILocale> GetLocales(object handle);

    void DeleteLocale(object handle, int id);

    ITextResource NewTextResource(object handle, ICategory category, string msgId);

    ITextResource? TryGetTextResource(object handle, ICategory category, string msgId);

    ITextResource GetOrCreateTextResource(object handle, ICategory category, string msgId);

    void DeleteTextResource(object handle, int id);

    void LocalizeText(object handle, ITextResource resource, ILocale locale, string translation);

    IEnumerable<KeyValuePair<ITextResource, string>> GetTranslation(object handle, string locale);
}