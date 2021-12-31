using System.Collections;
using System.Data;

namespace zcfux.SqlMapper;

internal sealed class LazyDataReader<T> : IEnumerable<T> where T : new()
{
    readonly IDataReader _reader;

    public LazyDataReader(IDataReader reader)
        => _reader = reader;

    public IEnumerator<T> GetEnumerator()
    {
        while (true)
        {
            var obj = _reader.ReadAndMap<T>();

            if (obj == null)
            {
                break;
            }

            yield return obj;
        }

        _reader.Close();
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}