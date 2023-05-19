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
using LinqToDB;
using LinqToDB.Mapping;

namespace zcfux.Data.Test.LinqToDB;

public class Engine : Data.LinqToDB.Engine
{
    public Engine(DataOptions opts)
        : base(opts)
    {
    }

    public override void Setup()
    {
        var mappingSchema = new MappingSchema();

        var builder = new FluentMappingBuilder(mappingSchema);

        builder.Entity<Model>()
            .HasSchemaName("test")
            .HasTableName("Model")
            .Property(m => m.ID).IsPrimaryKey()
            .Property(m => m.Value);

        builder.Build();

        MappingSchema.Default = builder.MappingSchema;
    }
}