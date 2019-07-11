using Microsoft.Analytics.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLakeAnalytics.ClassLibrary.Reducers
{
    public class EventReducer : IReducer
    {
        public override IEnumerable<IRow> Reduce(IRowset input, IUpdatableRow output)
        {
            foreach (var row in input.Rows)
            {
                foreach (var column in output.Schema)
                {
                    if (!column.IsReadOnly)
                    {
                        output.Set<string>(column.Name, row.Get<string>(column.Name));
                    }
                }
            }

            yield return output.AsReadOnly();
        }
    }
}
