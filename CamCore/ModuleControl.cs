using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace CamCore
{
    public abstract class ModuleControl : UserControl, IDisposable
    {
        public virtual void Dispose() { }
    }
}
