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
namespace zcfux.JobRunner;

public abstract class AJob : IJobDetails
{
    public AJob()
    {
    }

    public virtual void Freeze()
    {
    }

    public void Restore(IJobDetails jobDetails)
    {
        Guid = jobDetails.Guid;
        Status = jobDetails.Status;
        Created = jobDetails.Created;
        Args = jobDetails.Args;
        LastDone = jobDetails.LastDone;
        NextDue = jobDetails.NextDue;
        Errors = jobDetails.Errors;

        Restore();
    }

    protected virtual void Restore()
    {
    }

    public Guid Guid { get; internal set; }

    public EStatus Status { get; internal set; }

    public DateTime Created { get; internal set; }

    public string[]? Args { get; internal set; }

    public DateTime? LastDone { get; internal set; }

    public DateTime? NextDue { get; internal set; }

    public int Errors { get; internal set; }

    public bool IsDue => NextDue.HasValue && NextDue.Value <= DateTime.UtcNow;

    internal void Run()
    {
        if (Status == EStatus.Active && IsDue)
        {
            Action();
        }
    }

    protected abstract void Action();

    public void Done()
    {
        LastDone = DateTime.UtcNow;
        Errors = 0;
        Status = EStatus.Done;

        NextDue = Schedule();
    }

    protected virtual DateTime? Schedule()
        => null;

    public void Fail(int retrySecs)
    {
        Errors += 1;
        NextDue = DateTime.UtcNow.AddSeconds(retrySecs);
        Status = EStatus.Active;
    }

    public void Abort()
    {
        NextDue = null;
        Status = EStatus.Aborted;
    }
}