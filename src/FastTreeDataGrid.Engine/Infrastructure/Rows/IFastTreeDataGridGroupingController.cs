using System;
using System.Collections.Generic;

namespace FastTreeDataGrid.Engine.Infrastructure;

public interface IFastTreeDataGridGroupingController
{
    void ExpandAllGroups();

    void CollapseAllGroups();

    void ApplyGroupExpansionLayout(IEnumerable<FastTreeDataGridGroupingExpansionState> states, bool defaultExpanded)
    {
        if (states is null)
        {
            throw new ArgumentNullException(nameof(states));
        }

        if (defaultExpanded)
        {
            ExpandAllGroups();
        }
        else
        {
            CollapseAllGroups();
        }
    }
}
