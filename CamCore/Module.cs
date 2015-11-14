using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace CamCore
{
    public abstract class Module : IDisposable
    {
        public virtual string Name { get { return "Invalid Module"; } } // Name which user sees
        public virtual string FailText { get; protected set; } // Reason for module failure (on start/end)
        public virtual UserControl MainPanel { get; } // Control to be show on main window
        
        public abstract bool StartModule(); // When module is choosen from menu -> return false if cannot be started
        public abstract bool EndModule(); // When module is switched to other one -> return false if cannot be ended

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
