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

namespace zcfux.JobRunner.Data.LinqToDB;

[Table(Schema = "scheduler", Name = "Job")]
internal class JobRelation : IJobDetails
{
    public JobRelation()
    {
    }

    public JobRelation(IJobDetails jobDetails)
    {
        Guid = jobDetails.Guid;
        Status = jobDetails.Status;
        Created = jobDetails.Created;
        InitParams = jobDetails.InitParams;
        Args = jobDetails.Args;
        LastDone = jobDetails.LastDone;
        NextDue = jobDetails.NextDue;
        Errors = jobDetails.Errors;
    }

    [Column(Name = "Guid"), PrimaryKey]
    public Guid Guid { get; set; }

    [Column(Name = "KindId")]
    public int KindId { get; set; }

    [Association(ThisKey = "KindId", OtherKey = "Id")]
    public JobKindRelation? Kind { get; set; }

    [Column(Name = "Status")]
    public EStatus Status { get; set; }

    [Column(Name = "Running")]
    public bool Running { get; set; }

    [Column(Name = "Created")]
    public DateTime Created { get; set; }

    [Column(Name = "InitParams")]
    public string[]? InitParams { get; set; } = Array.Empty<string>();
    
    [Column(Name = "Args")]
    public string[]? Args { get; set; } = Array.Empty<string>();

    [Column(Name = "LastDone")]
    public DateTime? LastDone { get; set; }

    [Column(Name = "NextDue")]
    public DateTime? NextDue { get; set; }

    [Column(Name = "Errors")]
    public int Errors { get; set; }

    public AJob? NewJobInstance()
    {
        var instance = Activator.CreateInstance(Kind!.Assembly, Kind.FullName)?.Unwrap();

        var job = (instance as AJob);

        return job;
    }
}