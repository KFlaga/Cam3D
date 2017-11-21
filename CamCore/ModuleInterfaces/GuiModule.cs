using System;
using System.Windows.Controls;

namespace CamCore
{
    public abstract class GuiModule : IDisposable
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
            
        }

        public void Dispose()
        {
            if(!disposedValue)
            {
                Dispose(true);
                disposedValue = true;
            }
        }
        #endregion
    }
}
