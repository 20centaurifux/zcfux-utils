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
    Lesser General Public License for more detail

    You should have received a copy of the GNU Lesser General Public License
    along with this program; if not, write to the Free Software Foundation,
    Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 ***************************************************************************/
namespace zcfux.Tracking.Formatters;

internal static class Factory
{
    public static IFormatter CreateFormatter(FormatterAttribute attr)
    {
        var formatter = Activator.CreateInstance(attr.Assembly, attr.Name)?.Unwrap()!;

        CopyProperties(attr, formatter);

        return (formatter as IFormatter)!;
    }

    static void CopyProperties(object attr, object formatter)
    {
        var formatterType = formatter.GetType();

        foreach (var getter in attr.GetType().GetProperties())
        {
            var setter = formatterType.GetProperty(getter.Name);

            if (setter != null)
            {
                var value = getter.GetValue(attr);

                setter.SetValue(formatter, value);
            }
        }
    }
}