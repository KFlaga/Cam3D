using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamMain.ProcessingChain
{
    // Consider using System.ComponentModel.Design.ServiceContainer
    public class GlobalData
    {
        private Dictionary<Type, object> _data = new Dictionary<Type, object>();

        public T Get<T>() 
        {
            object val;
            if(_data.TryGetValue(typeof(T), out val))
            {
                return (T)val;
            }
            else
            {
                throw new KeyNotFoundException("Data of type: " + typeof(T).ToString() + " not found in GlobalData");
            }
        }

        public void Set<T>(T obj)
        {
            _data.Add(typeof(T), obj);
        }
    }
}
